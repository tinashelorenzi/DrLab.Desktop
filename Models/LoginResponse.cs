// Models/LoginResponse.cs - Simplified to match Django backend
using System.Collections.Generic;

namespace DrLab.Desktop.Models
{
    // Removed LoginRequest class - using anonymous object instead

    public class LoginResponse
    {
        public string message { get; set; } = "";
        public TokenData tokens { get; set; } = new();
        public UserProfile user { get; set; } = new();
        public string session_id { get; set; } = "";
        public bool device_trusted { get; set; }
        public bool must_change_password { get; set; }
    }

    public class TokenData
    {
        public string access { get; set; } = "";
        public string refresh { get; set; } = "";
    }

    public class UserProfile
    {
        public int id { get; set; }
        public string employee_id { get; set; } = "";
        public UserData user { get; set; } = new();
        public Department department { get; set; } = new();
        public string job_title { get; set; } = "";
        public bool is_active_employee { get; set; }
        public string hire_date { get; set; } = "";
        public string? termination_date { get; set; }
        public string phone { get; set; } = "";
        public string mobile { get; set; } = "";
        public string? signature_image { get; set; }
        public string? signature_date { get; set; }
        public string sanas_registration_number { get; set; } = "";
        public Dictionary<string, object> training_records { get; set; } = new();
        public Dictionary<string, object> competency_assessments { get; set; } = new();
        public string? last_competency_review { get; set; }
        public List<object> active_roles { get; set; } = new();
        public Dictionary<string, object> permissions { get; set; } = new();
        public List<string> department_access { get; set; } = new();
        public bool can_sign_reports { get; set; }
        public string preferred_language { get; set; } = "";
        public string timezone { get; set; } = "";
        public bool email_notifications { get; set; }
        public bool must_change_password { get; set; }
        public string? created_by { get; set; }
        public string created_at { get; set; } = "";
        public string updated_at { get; set; } = "";
    }

    public class UserData
    {
        public int id { get; set; }
        public string username { get; set; } = "";
        public string email { get; set; } = "";
        public string first_name { get; set; } = "";
        public string last_name { get; set; } = "";
        public string full_name { get; set; } = "";
        public string? last_login { get; set; }
    }

    public class Department
    {
        public string code { get; set; } = "";
        public string name { get; set; } = "";
        public string description { get; set; } = "";
        public bool is_testing_department { get; set; }
        public string? manager { get; set; }
        public int employee_count { get; set; }
        public string created_at { get; set; } = "";
        public string updated_at { get; set; } = "";
    }
}