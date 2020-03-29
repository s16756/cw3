using System.Collections.Generic;
using System.Linq;
using WebApplication.Models;

namespace WebApplication.DAL
{
    public class MockDbService : IDbService
    {
        private static List<Student> Students = new List<Student>()
        {
            new Student()
            {
                Id = 1,
                FirstName = "Jan",
                LastName = "Kowalski",
            },
            new Student()
            {
                Id = 2,
                FirstName = "Anna",
                LastName = "Malewski",
            },
            new Student()
            {
                Id = 3,
                FirstName = "Andrzej",
                LastName = "Andrzejewicz",
            }
        };
        
        public IEnumerable<Student> GetStudents()
        {
            return Students;
        }
    }
}