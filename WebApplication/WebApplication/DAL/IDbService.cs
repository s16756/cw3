using System.Collections.Generic;
using WebApplication.Models;

namespace WebApplication.DAL
{
    public interface IDbService
    {
        public IEnumerable<Student> GetStudents();

        public IEnumerable<Student> GetStudent(string indexNumber);
    }
}