
using System;

namespace SiteDataClasses
{
	[Serializable]
	public class RegisterRequest
	{
		public string username;
		public string pin;
		public string securityQuestionId;
		public string securityAnswer;
		public string avatarId;
	}

	[Serializable]
	public class LoginRequest
	{
		public string username;
		public string pin;
		public string securityQuestionId;
		public string securityAnswer;
	}

	[Serializable]
	public class AuthResponse
	{
		public bool success;
		public string message;
		public string token;
		public UserData user;
	}
}
