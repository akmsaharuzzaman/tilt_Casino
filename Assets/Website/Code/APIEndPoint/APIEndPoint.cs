
public static class APIEndPoint
{
	// Base URL
	public const string baseURL = "http://localhost:3000";

	// Health
	public const string health = "/";

	// Authentication
	public const string getSecurityQuestions = "/api/auth/security-questions";
	public const string register = "/api/auth/register";
	public const string login = "/api/auth/login";

	//Other
	public const string getBalance = "/api/auth/get-balance";

	// Full URLs
	public static string HealthURL => baseURL + health;

	public static string SecurityQuestionsURL => baseURL + getSecurityQuestions;

	public static string RegisterURL => baseURL + register;

	public static string LoginURL => baseURL + login;
}
