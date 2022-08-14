#if ALTCOINS
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BTCPayServer.Abstractions.Constants;
using BTCPayServer.Abstractions.Extensions;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Client;
using BTCPayServer.Data;
using BTCPayServer.Filters;
using BTCPayServer.Models;
using BTCPayServer.Payments;
using BTCPayServer.Security;
using BTCPayServer.Services.Altcoins.Wownero.Configuration;
using BTCPayServer.Services.Altcoins.Wownero.Payments;
using BTCPayServer.Services.Altcoins.Wownero.RPC.Models;
using BTCPayServer.Services.Altcoins.Wownero.Services;
using BTCPayServer.Services.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BTCPayServer.Services.Altcoins.Wownero.UI
{
    [Route("stores/{storeId}/wownerolike")]
    [OnlyIfSupportAttribute("XMR")]
    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Cookie)]
    [Authorize(Policy = Policies.CanModifyStoreSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
    [Authorize(Policy = Policies.CanModifyServerSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
    public class UIWowneroLikeStoreController : Controller
    {
        private readonly WowneroLikeConfiguration _WowneroLikeConfiguration;
        private readonly StoreRepository _StoreRepository;
        private readonly WowneroRPCProvider _WowneroRpcProvider;
        private readonly BTCPayNetworkProvider _BtcPayNetworkProvider;

        public UIWowneroLikeStoreController(WowneroLikeConfiguration wowneroLikeConfiguration,
            StoreRepository storeRepository, WowneroRPCProvider wowneroRpcProvider,
            BTCPayNetworkProvider btcPayNetworkProvider)
        {
            _WowneroLikeConfiguration = wowneroLikeConfiguration;
            _StoreRepository = storeRepository;
            _WowneroRpcProvider = wowneroRpcProvider;
            _BtcPayNetworkProvider = btcPayNetworkProvider;
        }

        public StoreData StoreData => HttpContext.GetStoreData();

        [HttpGet()]
        public async Task<IActionResult> GetStoreWowneroLikePaymentMethods()
        {
            var wownero = StoreData.GetSupportedPaymentMethods(_BtcPayNetworkProvider)
                .OfType<WowneroSupportedPaymentMethod>();

            var excludeFilters = StoreData.GetStoreBlob().GetExcludedPaymentMethods();

            var accountsList = _WowneroLikeConfiguration.WowneroLikeConfigurationItems.ToDictionary(pair => pair.Key,
                pair => GetAccounts(pair.Key));

            await Task.WhenAll(accountsList.Values);
            return View(new WowneroLikePaymentMethodListViewModel()
            {
                Items = _WowneroLikeConfiguration.WowneroLikeConfigurationItems.Select(pair =>
                    GetWowneroLikePaymentMethodViewModel(wownero, pair.Key, excludeFilters,
                        accountsList[pair.Key].Result))
            });
        }

        private Task<GetAccountsResponse> GetAccounts(string cryptoCode)
        {
            try
            {
                if (_WowneroRpcProvider.Summaries.TryGetValue(cryptoCode, out var summary) && summary.WalletAvailable)
                {

                    return _WowneroRpcProvider.WalletRpcClients[cryptoCode].SendCommandAsync<GetAccountsRequest, GetAccountsResponse>("get_accounts", new GetAccountsRequest());
                }
            }
            catch { }
            return Task.FromResult<GetAccountsResponse>(null);
        }

        private WowneroLikePaymentMethodViewModel GetWowneroLikePaymentMethodViewModel(
            IEnumerable<WowneroSupportedPaymentMethod> wownero, string cryptoCode,
            IPaymentFilter excludeFilters, GetAccountsResponse accountsResponse)
        {
            var settings = wownero.SingleOrDefault(method => method.CryptoCode == cryptoCode);
            _WowneroRpcProvider.Summaries.TryGetValue(cryptoCode, out var summary);
            _WowneroLikeConfiguration.WowneroLikeConfigurationItems.TryGetValue(cryptoCode,
                out var configurationItem);
            var fileAddress = Path.Combine(configurationItem.WalletDirectory, "wallet");
            var accounts = accountsResponse?.SubaddressAccounts?.Select(account =>
                new SelectListItem(
                    $"{account.AccountIndex} - {(string.IsNullOrEmpty(account.Label) ? "No label" : account.Label)}",
                    account.AccountIndex.ToString(CultureInfo.InvariantCulture)));
            return new WowneroLikePaymentMethodViewModel()
            {
                WalletFileFound = System.IO.File.Exists(fileAddress),
                Enabled =
                    settings != null &&
                    !excludeFilters.Match(new PaymentMethodId(cryptoCode, WowneroPaymentType.Instance)),
                Summary = summary,
                CryptoCode = cryptoCode,
                AccountIndex = settings?.AccountIndex ?? accountsResponse?.SubaddressAccounts?.FirstOrDefault()?.AccountIndex ?? 0,
                Accounts = accounts == null ? null : new SelectList(accounts, nameof(SelectListItem.Value),
                    nameof(SelectListItem.Text))
            };
        }

        [HttpGet("{cryptoCode}")]
        public async Task<IActionResult> GetStoreWowneroLikePaymentMethod(string cryptoCode)
        {
            cryptoCode = cryptoCode.ToUpperInvariant();
            if (!_WowneroLikeConfiguration.WowneroLikeConfigurationItems.ContainsKey(cryptoCode))
            {
                return NotFound();
            }

            var vm = GetWowneroLikePaymentMethodViewModel(StoreData.GetSupportedPaymentMethods(_BtcPayNetworkProvider)
                    .OfType<WowneroSupportedPaymentMethod>(), cryptoCode,
                StoreData.GetStoreBlob().GetExcludedPaymentMethods(), await GetAccounts(cryptoCode));
            return View(nameof(GetStoreWowneroLikePaymentMethod), vm);
        }

        [HttpPost("{cryptoCode}")]
        public async Task<IActionResult> GetStoreWowneroLikePaymentMethod(WowneroLikePaymentMethodViewModel viewModel, string command, string cryptoCode)
        {
            cryptoCode = cryptoCode.ToUpperInvariant();
            if (!_WowneroLikeConfiguration.WowneroLikeConfigurationItems.TryGetValue(cryptoCode,
                out var configurationItem))
            {
                return NotFound();
            }

            if (command == "add-account")
            {
                try
                {
                    var newAccount = await _WowneroRpcProvider.WalletRpcClients[cryptoCode].SendCommandAsync<CreateAccountRequest, CreateAccountResponse>("create_account", new CreateAccountRequest()
                    {
                        Label = viewModel.NewAccountLabel
                    });
                    viewModel.AccountIndex = newAccount.AccountIndex;
                }
                catch (Exception)
                {
                    ModelState.AddModelError(nameof(viewModel.AccountIndex), "Could not create a new account.");
                }

            }
            else if (command == "upload-wallet")
            {
                var valid = true;
                if (viewModel.WalletFile == null)
                {
                    ModelState.AddModelError(nameof(viewModel.WalletFile), "Please select the view-only wallet file");
                    valid = false;
                }
                if (viewModel.WalletKeysFile == null)
                {
                    ModelState.AddModelError(nameof(viewModel.WalletKeysFile), "Please select the view-only wallet keys file");
                    valid = false;
                }

                if (valid)
                {
                    if (_WowneroRpcProvider.Summaries.TryGetValue(cryptoCode, out var summary))
                    {
                        if (summary.WalletAvailable)
                        {
                            TempData.SetStatusMessageModel(new StatusMessageModel()
                            {
                                Severity = StatusMessageModel.StatusSeverity.Error,
                                Message = $"There is already an active wallet configured for {cryptoCode}. Replacing it would break any existing invoices!"
                            });
                            return RedirectToAction(nameof(GetStoreWowneroLikePaymentMethod),
                                new { cryptoCode });
                        }
                    }

                    var fileAddress = Path.Combine(configurationItem.WalletDirectory, "wallet");
                    using (var fileStream = new FileStream(fileAddress, FileMode.Create))
                    {
                        await viewModel.WalletFile.CopyToAsync(fileStream);
                        try
                        {
                            Exec($"chmod 666 {fileAddress}");
                        }
                        catch
                        {
                        }
                    }

                    fileAddress = Path.Combine(configurationItem.WalletDirectory, "wallet.keys");
                    using (var fileStream = new FileStream(fileAddress, FileMode.Create))
                    {
                        await viewModel.WalletKeysFile.CopyToAsync(fileStream);
                        try
                        {
                            Exec($"chmod 666 {fileAddress}");
                        }
                        catch
                        {
                        }

                    }

                    fileAddress = Path.Combine(configurationItem.WalletDirectory, "password");
                    using (var fileStream = new StreamWriter(fileAddress, false))
                    {
                        await fileStream.WriteAsync(viewModel.WalletPassword);
                        try
                        {
                            Exec($"chmod 666 {fileAddress}");
                        }
                        catch
                        {
                        }
                    }

                    return RedirectToAction(nameof(GetStoreWowneroLikePaymentMethod), new
                    {
                        cryptoCode,
                        StatusMessage = "View-only wallet files uploaded. If they are valid the wallet will soon become available."

                    });
                }
            }

            if (!ModelState.IsValid)
            {

                var vm = GetWowneroLikePaymentMethodViewModel(StoreData
                        .GetSupportedPaymentMethods(_BtcPayNetworkProvider)
                        .OfType<WowneroSupportedPaymentMethod>(), cryptoCode,
                    StoreData.GetStoreBlob().GetExcludedPaymentMethods(), await GetAccounts(cryptoCode));

                vm.Enabled = viewModel.Enabled;
                vm.NewAccountLabel = viewModel.NewAccountLabel;
                vm.AccountIndex = viewModel.AccountIndex;
                return View(vm);
            }

            var storeData = StoreData;
            var blob = storeData.GetStoreBlob();
            storeData.SetSupportedPaymentMethod(new WowneroSupportedPaymentMethod()
            {
                AccountIndex = viewModel.AccountIndex,
                CryptoCode = viewModel.CryptoCode
            });

            blob.SetExcluded(new PaymentMethodId(viewModel.CryptoCode, WowneroPaymentType.Instance), !viewModel.Enabled);
            storeData.SetStoreBlob(blob);
            await _StoreRepository.UpdateStore(storeData);
            return RedirectToAction("GetStoreWowneroLikePaymentMethods",
                new { StatusMessage = $"{cryptoCode} settings updated successfully", storeId = StoreData.Id });
        }

        private void Exec(string cmd)
        {

            var escapedArgs = cmd.Replace("\"", "\\\"", StringComparison.InvariantCulture);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "/bin/sh",
                    Arguments = $"-c \"{escapedArgs}\""
                }
            };

#pragma warning disable CA1416 // Validate platform compatibility
            process.Start();
#pragma warning restore CA1416 // Validate platform compatibility
            process.WaitForExit();
        }

        public class WowneroLikePaymentMethodListViewModel
        {
            public IEnumerable<WowneroLikePaymentMethodViewModel> Items { get; set; }
        }

        public class WowneroLikePaymentMethodViewModel
        {
            public WowneroRPCProvider.WowneroLikeSummary Summary { get; set; }
            public string CryptoCode { get; set; }
            public string NewAccountLabel { get; set; }
            public long AccountIndex { get; set; }
            public bool Enabled { get; set; }

            public IEnumerable<SelectListItem> Accounts { get; set; }
            public bool WalletFileFound { get; set; }
            [Display(Name = "View-Only Wallet File")]
            public IFormFile WalletFile { get; set; }
            public IFormFile WalletKeysFile { get; set; }
            public string WalletPassword { get; set; }
        }
    }
}
#endif
