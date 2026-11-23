
using System;

namespace SiteDataClasses
{
	[Serializable]
	public class SecurityQuestionData
	{
		public string id;
		public string question;
	}

	[Serializable]
	public class SecurityQuestionResponse
	{
		public bool success;
		public string message;
		public SecurityQuestionData[] questions;
	}
}
