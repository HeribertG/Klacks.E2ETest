// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.E2ETest.Chatbot.Helpers;
using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Microsoft.Playwright;

namespace Klacks.E2ETest.Klacksy
{
    /// <summary>
    /// Investigates whether the "Während du weg warst..." proactive-inbox overlay card survives a
    /// normal F5 reload and a cache-clearing hard reset. Seeds one unread agent_trigger_dispatches
    /// row directly in the dev DB (cleaned up in TearDown by its own dedup_key prefix) so the card
    /// has something to show, then opens the assistant aside before and after each reload kind.
    /// </summary>
    [TestFixture]
    [Order(96)]
    [Category("Klacksy")]
    public class ProactiveInboxReloadTest : PlaywrightSetup
    {
        private const string AssistantToggleButtonId = "header-assistant-button";
        private const string InboxHeadingAriaLabel = "Während du weg warst…";
        private const string DedupKeyPrefix = "E2E_PROACTIVE_INBOX_RELOAD_TEST_";

        private string _userId = string.Empty;

        [OneTimeSetUp]
        public async Task ResolveUserIdAsync()
        {
            var result = await DbHelper.ExecuteSqlAsync(
                $"SELECT id FROM \"AspNetUsers\" WHERE email = '{Escape(UserName)}'");
            Assert.That(result, Does.Not.StartWith("ERROR:"), $"Could not resolve user id: {result}");
            Assert.That(result, Is.Not.Empty, $"No AspNetUsers row for '{UserName}'");
            _userId = result.Trim();
        }

        [TearDown]
        public async Task CleanupSeededRowsAsync()
        {
            await DbHelper.ExecuteSqlAsync(
                $"DELETE FROM agent_trigger_dispatches WHERE dedup_key LIKE '{DedupKeyPrefix}%'");
        }

        private static string Escape(string value) => value.Replace("'", "''");

        private async Task<string> SeedOneUnreadInboxRowAsync()
        {
            var dedupKey = DedupKeyPrefix + Guid.NewGuid().ToString("N");
            var sql = $@"
                INSERT INTO agent_trigger_dispatches (
                  id, user_id, trigger_kind, dedup_key, create_time, is_deleted,
                  content_key, content_params_json, reaction, severity, read_at_utc, reminder_count
                ) VALUES (
                  gen_random_uuid(), '{Escape(_userId)}', 'e2e_manual_test', '{Escape(dedupKey)}', now(), false,
                  'i18n:assistant.proactive.periodOverdue', '{{}}', 0, 'medium', NULL, 0
                );";
            var result = await DbHelper.ExecuteSqlAsync(sql);
            Assert.That(result, Does.Not.StartWith("ERROR:"), $"Seeding failed: {result}");
            return dedupKey;
        }

        private async Task<bool> IsAsideOpenAsync()
        {
            return await Actions.IsElementVisibleById("aside-close-btn");
        }

        private async Task OpenAssistantAsideAsync()
        {
            if (!await IsAsideOpenAsync())
            {
                await Actions.ClickButtonById(AssistantToggleButtonId);
                await Actions.Wait1000();
            }
        }

        private async Task<bool> InboxPanelVisibleAsync()
        {
            var count = await Actions.CountElementsBySelector($"button[aria-label='{InboxHeadingAriaLabel}']");
            return count > 0;
        }

        /// <summary>
        /// Chromium hard reset (Ctrl+Shift+R): bypass HTTP cache for every subsequent request and
        /// clear whatever is already cached, then reload. No Actions equivalent exists for this -
        /// it is browser-level cache control, not a page interaction.
        /// </summary>
        private async Task HardResetAsync()
        {
            var cdp = await Page.Context.NewCDPSessionAsync(Page);
            await cdp.SendAsync("Network.setCacheDisabled", new Dictionary<string, object> { ["cacheDisabled"] = true });
            await cdp.SendAsync("Network.clearBrowserCache");
            await Page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        }

        [Test]
        [Order(1)]
        public async Task Step1_PanelAppearsOnFirstOpenBeforeAnyReload()
        {
            TestContext.Out.WriteLine("=== Step 1: seed one unread row, open aside, expect the panel ===");

            await SeedOneUnreadInboxRowAsync();
            await OpenAssistantAsideAsync();
            await Actions.Wait2000();

            var visible = await InboxPanelVisibleAsync();
            TestContext.Out.WriteLine($"Inbox panel visible on first open: {visible}");
            Assert.That(visible, Is.True, "Inbox panel did not appear even before any reload - not a reload-specific bug.");
        }

        [Test]
        [Order(2)]
        public async Task Step2_PanelReappearsAfterNormalF5Reload()
        {
            TestContext.Out.WriteLine("=== Step 2: seed a row, reload (F5), reopen aside, expect the panel ===");

            await SeedOneUnreadInboxRowAsync();

            await Actions.Reload();
            await Actions.Wait2000();

            var visibleBeforeReopen = await InboxPanelVisibleAsync();
            TestContext.Out.WriteLine($"Inbox panel visible right after F5, aside not yet reopened: {visibleBeforeReopen}");

            await OpenAssistantAsideAsync();
            await Actions.Wait2000();

            var visibleAfterReopen = await InboxPanelVisibleAsync();
            TestContext.Out.WriteLine($"Inbox panel visible after F5 + reopening the aside: {visibleAfterReopen}");
            Assert.That(visibleAfterReopen, Is.True, "Inbox panel did not reappear after a normal F5 reload.");
        }

        [Test]
        [Order(3)]
        public async Task Step3_PanelReappearsAfterHardReset()
        {
            TestContext.Out.WriteLine("=== Step 3: seed a row, hard reset (cache-clearing reload), reopen aside, expect the panel ===");

            await SeedOneUnreadInboxRowAsync();

            await HardResetAsync();
            await Actions.Wait2000();

            var visibleBeforeReopen = await InboxPanelVisibleAsync();
            TestContext.Out.WriteLine($"Inbox panel visible right after hard reset, aside not yet reopened: {visibleBeforeReopen}");

            await OpenAssistantAsideAsync();
            await Actions.Wait2000();

            var visibleAfterReopen = await InboxPanelVisibleAsync();
            TestContext.Out.WriteLine($"Inbox panel visible after hard reset + reopening the aside: {visibleAfterReopen}");
            Assert.That(visibleAfterReopen, Is.True, "Inbox panel did not reappear after a hard reset.");
        }

        /// <summary>
        /// Regression guard for the fix in ChatMessageActionsService.presentInboxMessages(): merely
        /// displaying a row (inboxExpanded() defaults to true on every fresh page load) must no
        /// longer mark it read as a silent side effect. Before the fix, the very first time a user
        /// opened the assistant after a proactive push, the row was marked read just by being shown
        /// - so a later reload legitimately found nothing left (UnreadOnly) and the panel stayed
        /// gone, which is what the user reported as "disappears after F5". Now: view once, close,
        /// reload, reopen - the row must still be unread in the DB and the panel must still appear.
        /// </summary>
        [Test]
        [Order(4)]
        public async Task Step4_ViewingThePanelOnceDoesNotSilentlyMarkTheRowRead()
        {
            TestContext.Out.WriteLine("=== Step 4: view the panel once, reload, expect it to still be there ===");

            var dedupKey = await SeedOneUnreadInboxRowAsync();

            await OpenAssistantAsideAsync();
            await Actions.Wait2000();
            Assert.That(await InboxPanelVisibleAsync(), Is.True, "Panel must appear on first view for this scenario to be meaningful.");

            var readAtAfterFirstView = await DbHelper.ExecuteSqlAsync(
                $"SELECT read_at_utc FROM agent_trigger_dispatches WHERE dedup_key = '{Escape(dedupKey)}'");
            TestContext.Out.WriteLine($"read_at_utc right after first view (must stay empty): '{readAtAfterFirstView}'");
            Assert.That(readAtAfterFirstView, Is.Empty,
                "Row was marked read merely by being displayed - viewing must never substitute for a conscious dismiss.");

            await Actions.Reload();
            await Actions.Wait2000();

            await OpenAssistantAsideAsync();
            await Actions.Wait2000();

            var visibleAfterReopen = await InboxPanelVisibleAsync();
            TestContext.Out.WriteLine($"Inbox panel visible after F5 + reopen, never explicitly dismissed: {visibleAfterReopen}");
            Assert.That(visibleAfterReopen, Is.True,
                "A row must stay unread (and the panel must keep reappearing) until the user explicitly dismisses it.");
        }

        /// <summary>
        /// The explicit counterpart to Step 4: only a conscious dismiss (per-row "Ausblenden") may
        /// mark a row read. Once dismissed, it is correctly gone for good - including across reload.
        /// </summary>
        [Test]
        [Order(5)]
        public async Task Step5_ExplicitDismissMarksItReadAndItStaysGoneAfterReload()
        {
            TestContext.Out.WriteLine("=== Step 5: explicitly dismiss the row, reload, expect it to stay gone ===");

            var dedupKey = await SeedOneUnreadInboxRowAsync();

            await OpenAssistantAsideAsync();
            await Actions.Wait2000();
            Assert.That(await InboxPanelVisibleAsync(), Is.True, "Panel must appear before it can be dismissed.");

            await DismissTheOneInboxRowAsync();

            var readAtAfterDismiss = await DbHelper.ExecuteSqlAsync(
                $"SELECT read_at_utc FROM agent_trigger_dispatches WHERE dedup_key = '{Escape(dedupKey)}'");
            TestContext.Out.WriteLine($"read_at_utc right after explicit dismiss: '{readAtAfterDismiss}'");
            Assert.That(readAtAfterDismiss, Is.Not.Empty, "An explicit dismiss must mark the row read.");

            await Actions.Reload();
            await Actions.Wait2000();
            await OpenAssistantAsideAsync();
            await Actions.Wait2000();

            var visibleAfterReopen = await InboxPanelVisibleAsync();
            TestContext.Out.WriteLine($"Inbox panel visible after F5 + reopen, explicitly dismissed: {visibleAfterReopen}");
            Assert.That(visibleAfterReopen, Is.False, "A dismissed row must stay gone.");
        }

        private const string DismissToggleAriaLabel = "Ausblenden";
        private const string DismissNoReasonLabel = "Ohne Grund ausblenden";

        private async Task DismissTheOneInboxRowAsync()
        {
            var toggle = await Actions.FindElementByCssSelector($"button[aria-label='{DismissToggleAriaLabel}']");
            Assert.That(toggle, Is.Not.Null, "Dismiss-menu toggle not found on the seeded inbox row.");
            await toggle!.ClickAsync();
            await Actions.Wait500();

            var noReasonOption = await Actions.FindElementByCssSelector(
                $".proactive-dismiss-menu [role='menuitem']:has-text('{DismissNoReasonLabel}')");
            Assert.That(noReasonOption, Is.Not.Null, "No-reason dismiss option not found in the dismiss menu.");
            await noReasonOption!.ClickAsync();
            await Actions.Wait500();
        }
    }
}
