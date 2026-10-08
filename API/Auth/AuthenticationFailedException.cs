namespace API.Auth;

// "We don't know who you are" — becomes a 401 in DomainExceptionHandler.
// Lives in API, not Domain: signing in is an application concern, not a
// rule about students or bursaries.
public sealed class AuthenticationFailedException(string message) : Exception(message);
