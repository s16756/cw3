using System;

namespace WebApplication.Models
{
    public class Student
    {
        public int Id { get; set; }
        
        public string FirstName { get; set; }

        public string LastName { get; set; }
        
        public string IndexNumber { get; set; }
        
        public DateTime BirthDate { get; set; }
        
        public string Semester { get; set; }
        
        public string StudyName { get; set; }
    }
}