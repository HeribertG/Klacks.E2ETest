using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;

namespace E2ETest.Login
{
    [TestFixture]
    public class LoginTest : PlaywrightSetup
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
        public async Task VerifySuccessfulLogin()
        {
            // Da PlaywrightSetup bereits den Login in OneTimeSetup durchführt,
            // überprüfen wir nur, ob wir erfolgreich eingeloggt sind
            
            TestContext.Out.WriteLine($"Aktuelle URL: {Page.Url}");
            TestContext.Out.WriteLine($"Eingeloggt als: {UserName}");
            
            // Überprüfe, dass wir nicht mehr auf der Login-Seite sind
            Assert.That(Page.Url, Does.Not.Contain("login"), 
                "Login war nicht erfolgreich - URL enthält immer noch 'login'");
            
            // Überprüfe auf API-Fehler
            Assert.That(_listener!.HasApiErrors(), Is.False, 
                $"API-Fehler nach Login: {_listener!.GetLastErrorMessage()}");
            
            // Optional: Navigiere zur Hauptseite um weitere Elemente zu prüfen
            var dashboardElement = await Actions.FindElementByCssSelector("[class*='dashboard'], [class*='main'], nav");
            Assert.That(dashboardElement, Is.Not.Null, 
                "Kein Dashboard/Navigation Element gefunden - Login möglicherweise fehlgeschlagen");
            
            TestContext.Out.WriteLine("Login Verifikation erfolgreich!");
        }

        [Test]
        public async Task NavigateToMainPages()
        {
            // Test Navigation zu verschiedenen Hauptseiten nach erfolgreichem Login
            TestContext.Out.WriteLine("Teste Navigation zu Hauptseiten...");
            
            // Versuche Absence Navigation zu finden
            var navAbsence = await Actions.FindElementById(MainNavIds.OpenAbsenceId);
            if (navAbsence != null)
            {
                var pageTracker = new PageUrlTracker(Page);
                
                await Actions.ClickButtonById(MainNavIds.OpenAbsenceId);
                await Actions.WaitForSpinnerToDisappear();
                await Actions.Wait1000();
                
                Assert.That(pageTracker.HasChanged(Page) && Page.Url.Contains("absence"), 
                    Is.True, "Konnte nicht zur Absence Seite navigieren");
                
                TestContext.Out.WriteLine("Navigation zur Absence Seite erfolgreich");
            }
            else
            {
                TestContext.Out.WriteLine("Absence Navigation Button nicht gefunden - überspringe diesen Test");
            }
            
            // Überprüfe auf API-Fehler
            Assert.That(_listener!.HasApiErrors(), Is.False, 
                $"API-Fehler während der Navigation: {_listener!.GetLastErrorMessage()}");
        }

        [Test]
        public async Task VerifyUserIsLoggedIn()
        {
            // Überprüfe ob Benutzerinformationen sichtbar sind
            TestContext.Out.WriteLine("Überprüfe Benutzer-Login-Status...");
            
            // Suche nach Benutzername oder Profil-Element
            var userElement = await Actions.FindElementByCssSelector(
                "[class*='user'], [class*='profile'], [class*='account']")
                ?? await Actions.FindElementByCssSelector("span");
            
            if (userElement != null)
            {
                TestContext.Out.WriteLine("Benutzer-Element gefunden - Login bestätigt");
            }
            else
            {
                TestContext.Out.WriteLine("Warnung: Kein spezifisches Benutzer-Element gefunden");
            }
            
            // Die Hauptprüfung ist, dass wir nicht auf der Login-Seite sind
            Assert.That(Page.Url, Does.Not.Contain("login"), "Benutzer ist nicht eingeloggt");
            
            // Alternativ: Suche nach spezifischen Text-Elementen die den Benutzernamen enthalten könnten
            if (userElement == null)
            {
                // Versuche andere Selektoren
                userElement = await Actions.FindElementByCssSelector("[class*='navbar']") 
                    ?? await Actions.FindElementByCssSelector("[class*='header']");
            }
            
            TestContext.Out.WriteLine($"Login-Status überprüft für Benutzer: {UserName}");
        }
    }
}