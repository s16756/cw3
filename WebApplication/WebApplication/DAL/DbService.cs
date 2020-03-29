using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using WebApplication.Models;

namespace WebApplication.DAL
{
    public class DbService : IDbService
    {
        private readonly IConfiguration _configuration;

        public DbService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IEnumerable<Student> GetStudents()
        {
            using var client = new SqlConnection(_configuration["ConnectionString"]);
            client.Open();
            using var command = new SqlCommand(@"
SELECT
[s].[FirstName],
[s].[LastName],
[s].[BirthDate],
[s].[IndexNumber],
[e].[Semester],
[st].[Name] AS [StudyName]
FROM [Student] [s]
INNER JOIN [Enrollment] [e] ON [s].[IdEnrollment] = [e].[IdEnrollment]
INNER JOIN [Studies] [st] ON [st].[IdStudy] = [e].[IdStudy];
", client);
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                yield return new Student
                {
                    IndexNumber = reader["IndexNumber"].ToString(),
                    FirstName = reader["FirstName"].ToString(),
                    LastName = reader["LastName"].ToString(),
                    BirthDate = DateTime.Parse(reader["BirthDate"].ToString()),
                    Semester = reader["Semester"].ToString(),
                    StudyName = reader["StudyName"].ToString()
                };
            }
            
            client.Close();
        }

        public IEnumerable<Student> GetStudent(string indexNumber)
        {
            using var client = new SqlConnection(_configuration["ConnectionString"]);
            client.Open();
            using var command = new SqlCommand(@"
SELECT
[s].[FirstName],
[s].[LastName],
[s].[BirthDate],
[s].[IndexNumber],
[e].[Semester],
[st].[Name] AS [StudyName]
FROM [Student] [s]
INNER JOIN [Enrollment] [e] ON [s].[IdEnrollment] = [e].[IdEnrollment]
INNER JOIN [Studies] [st] ON [st].[IdStudy] = [e].[IdStudy]
WHERE [s].[IndexNumber] = @IndexNumber
", client);
            command.Parameters.Add(new SqlParameter("IndexNumber", indexNumber));
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                yield return new Student
                {
                    IndexNumber = reader["IndexNumber"].ToString(),
                    FirstName = reader["FirstName"].ToString(),
                    LastName = reader["LastName"].ToString(),
                    BirthDate = DateTime.Parse(reader["BirthDate"].ToString()),
                    Semester = reader["Semester"].ToString(),
                    StudyName = reader["StudyName"].ToString()
                };
            }
            
            client.Close();
        }
    }
}