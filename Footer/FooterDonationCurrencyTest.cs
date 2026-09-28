// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/**
 * Donation dialog in the footer: CHF and EUR offer a payment QR code, USD has no QR standard and
 * shows the SWIFT transfer data (IBAN and BIC) instead.
 */

using Klacks.E2ETest.Helpers;

namespace Klacks.E2ETest.Footer;

[TestFixture]
[Category("Footer")]
public class FooterDonationCurrencyTest : PlaywrightSetup
{
    private const string DonationLinkId = "footer-donation-link";
    private const string CurrencyChfId = "donation-currency-chf";
    private const string CurrencyUsdId = "donation-currency-usd";
    private const string BicInputId = "donation-bic";
    private const string QrImageSelector = ".donation-qr img";
    private const string UsdHintSelector = ".donation-usd-hint";
    private const string CloseButtonId = "modal-donation-close";
    private const string ExpectedBic = "POFICHBEXXX";

    [Test]
    public async Task Usd_Shows_Swift_Data_Instead_Of_A_Qr_Code()
    {
        await Actions.WaitForSpinnerToDisappear();
        await Actions.ClickButtonById(DonationLinkId);
        await Actions.Wait1000();

        var chfQrCount = await Actions.CountElementsBySelector(QrImageSelector);
        var chfBicVisible = await Actions.IsElementVisibleById(BicInputId);

        await Actions.ClickButtonById(CurrencyUsdId);
        await Actions.Wait500();

        var usdQrCount = await Actions.CountElementsBySelector(QrImageSelector);
        var usdHintCount = await Actions.CountElementsBySelector(UsdHintSelector);
        var usdBic = await Actions.ReadInput(BicInputId);
        await Actions.TakeScreenshotAsync(Path.Combine(TestContext.CurrentContext.WorkDirectory, "donation-usd.png"));

        await Actions.ClickButtonById(CurrencyChfId);
        await Actions.Wait1000();
        var backToChfQrCount = await Actions.CountElementsBySelector(QrImageSelector);

        await Actions.ClickButtonById(CloseButtonId);

        Assert.Multiple(() =>
        {
            Assert.That(chfQrCount, Is.EqualTo(1), "CHF must show the Swiss QR code");
            Assert.That(chfBicVisible, Is.False, "CHF must not show the SWIFT BIC field");
            Assert.That(usdQrCount, Is.EqualTo(0), "USD has no QR standard, so no QR code may be shown");
            Assert.That(usdHintCount, Is.EqualTo(1), "USD must explain the SWIFT transfer");
            Assert.That(usdBic, Is.EqualTo(ExpectedBic), "USD must show the BIC for an international transfer");
            Assert.That(backToChfQrCount, Is.EqualTo(1), "switching back to CHF must restore the QR code");
        });
    }
}
