namespace AppRegistration.AppReg.Core
{
    internal class AppRegistrationExceptions
    {
        [Serializable]
        public class UniqueAppRegistrationNameNotFoundException : Exception
        {
            public UniqueAppRegistrationNameNotFoundException() { }

            public UniqueAppRegistrationNameNotFoundException(string message)
                : base(message) { }

            public UniqueAppRegistrationNameNotFoundException(string message, Exception inner)
                : base(message, inner) { }
        }
    }
}
