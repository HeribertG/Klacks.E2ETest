using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;

namespace E2ETest.CalendarGantt
{
    internal class CaledarGanttTest : PlaywrightSetup
    {
        private Listener? _listener;

        [SetUp]
        public void SetupInternal()
        {
            _listener = new Listener(Page);
            _listener.RecognizeApiErrors();
        }

        [TearDown]
        public async Task CleanupAfterTestAsync()
        {
            if (_listener != null)
            {
                await _listener.WaitForResponseHandlingAsync();
                if (_listener.HasApiErrors())
                {
                    TestContext.WriteLine(_listener.GetLastErrorMessage());
                }

                _listener?.ResetErrors();
            }

            _listener = null;
        }

        [Test]
        public async Task ShouldOpenGanttPageSuccessfully()
        {
            TestContext.Out.WriteLine($"Go to: {BaseUrl}");


            var navAbsence = await Actions.FindElementById(MainNavIds.OpenAbsenceId);

            if (navAbsence != null)
            {
                var pageTracker = new PageUrlTracker(Page);

                await Actions.ClickButtonById(MainNavIds.OpenAbsenceId);
                await Actions.WaitForSpinnerToDisappear();
                await Actions.Wait3500();

                Assert.That(pageTracker.HasChanged(Page) && Page.Url.Contains("absence"), Is.True, "Could not open Absence Page");
            }
            else
            {
                TestContext.Out.WriteLine("Absence Navigation Button not found");
            }

            Assert.That(_listener!.HasApiErrors(), Is.False, $"API errors occurred during test execution /n{_listener!.GetLastErrorMessage}");

            await Actions.Wait3500();

            await Actions.Wait3500();

            Assert.Pass();
        }
    }
}
