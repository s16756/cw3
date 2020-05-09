using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using WebApplication.Dtos;
using WebApplication.Models;

namespace WebApplication.DAL
{
    public class DbService : IDbService
    {
        private const string ProcedureName = "Promotions";
        private readonly IConfiguration _configuration;

        public DbService(IConfiguration configuration)
        {
            _configuration = configuration;
            
            // Create procedure
            using var client = new SqlConnection(_configuration["ConnectionString"]);
            client.Open();
            
            using var command = new SqlCommand($@"
CREATE OR ALTER PROCEDURE {ProcedureName}
    @StudiesId INT,
    @Semester INT
AS BEGIN
    DECLARE @NextEnrollmentId INT;
    SELECT @NextEnrollmentId = IdEnrollment FROM [Enrollment] WHERE [IdStudy] = @StudiesId AND Semester = @Semester + 1;
	IF @NextEnrollmentId IS NULL BEGIN
		INSERT INTO Enrollment (IdEnrollment, Semester, IdStudy, StartDate)
		SELECT MAX(IdEnrollment) + 1 AS IdEnrollment, @Semester + 1 as Semester, @StudiesId as IdStudy, GETDATE() as StartDate FROM Enrollment;

		SELECT @NextEnrollmentId = IdEnrollment FROM [Enrollment] WHERE [IdStudy] = @StudiesId AND Semester = @Semester + 1;
	END;

	UPDATE Student SET IdEnrollment = @NextEnrollmentId
	WHERE IdEnrollment = (SELECT TOP 1 IdEnrollment FROM [Enrollment] WHERE [IdStudy] = @StudiesId AND Semester = @Semester);
END
", client);
            command.ExecuteNonQuery();
            client.Close();
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

        public int? GetStudiesIdByName(string name)
        {
            using var client = new SqlConnection(_configuration["ConnectionString"]);
            client.Open();
            using var command = new SqlCommand(@"SELECT TOP 1 [IdStudy] FROM [Studies] WHERE [Name] = @Name;", client);
            command.Parameters.Add(new SqlParameter("Name", name));

            var result = (int?)command.ExecuteScalar();

            client.Close();

            return result;
        }

        public bool IsIndexNumberUnique(string indexNumber)
        {
            using var client = new SqlConnection(_configuration["ConnectionString"]);
            client.Open();
            
            using var command =
                new SqlCommand(
                    @"SELECT 1 FROM [Student] WHERE [IndexNumber] = @IndexNumber",
                    client);
            command.Parameters.Add(new SqlParameter("IndexNumber", indexNumber));
            var result = Convert.ToBoolean(command.ExecuteScalar());
            
            client.Close();
            return !result;
        }

        public int? GetEnrollmentByStudyIdAndSemester(int studyId, int semester)
        {
            using var client = new SqlConnection(_configuration["ConnectionString"]);
            client.Open();

            try
            {
                using var command =
                    new SqlCommand(
                        @"SELECT TOP 1 [IdEnrollment] FROM [Enrollment] WHERE [IdStudy] = @StudyId AND [Semester] = @Semester;",
                        client);
                command.Parameters.AddRange(new[]
                {
                    new SqlParameter("StudyId", studyId),
                    new SqlParameter("Semester", semester),
                });

                return (int?) command.ExecuteScalar();
            }
            finally
            {
                client.Close();
            }
        }

        public void CreateStudent(StudentCreateDto dto, int studiesId)
        {
            using var client = new SqlConnection(_configuration["ConnectionString"]);
            client.Open();
            using var transaction = client.BeginTransaction();
            try
            {
                var enrollmentId = GetEnrollmentByStudyIdAndSemester(studiesId, 1);

                if (!enrollmentId.HasValue)
                {
                    using var commandFindLatestId =
                        new SqlCommand("SELECT MAX([IdEnrollment]) FROM [Enrollment];", client, transaction);
                    var latestId = (int) commandFindLatestId.ExecuteScalar();

                    using var createEnrollmentCommand = new SqlCommand(@"
                        INSERT INTO [Enrollment](IdEnrollment, Semester, IdStudy, StartDate)
                        VALUES (@Id, 1, @IdStudy, GETDATE());
                    ", client, transaction);
                    createEnrollmentCommand.Parameters.Add(new SqlParameter("Id", latestId + 1));
                    createEnrollmentCommand.Parameters.Add(new SqlParameter("IdStudy", studiesId));

                    createEnrollmentCommand.ExecuteNonQuery();

                    enrollmentId = latestId + 1;
                }

                var dateSplitted = dto.BirthDate.Split('.');
                var day = int.Parse(dateSplitted[0]);
                var month = int.Parse(dateSplitted[1]);
                var year = int.Parse(dateSplitted[2]);
                var parsedDate = new DateTime(year, month, day);
                
                using var commandCreateStudent = 
                    new SqlCommand(@"
                    INSERT INTO [Student]([IndexNumber],[FirstName],[LastName],[BirthDate],[IdEnrollment])
                    VALUES (@IndexNumber, @FirstName, @LastName, @BirthDate, @EnrolId);
                    ", client, transaction);
                commandCreateStudent.Parameters.AddRange(new []
                {
                    new SqlParameter("@IndexNumber", dto.IndexNumber),
                    new SqlParameter("@FirstName", dto.FirstName),
                    new SqlParameter("@LastName", dto.LastName),
                    new SqlParameter("@BirthDate", parsedDate),
                    new SqlParameter("@EnrolId", enrollmentId)
                });

                commandCreateStudent.ExecuteNonQuery();

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
            }
            finally
            {
                client.Close();
            }
        }

        public void PromoteStudents(int studiesId, int semester)
        {
            using var client = new SqlConnection(_configuration["ConnectionString"]);
            client.Open();
            using var command = new SqlCommand(ProcedureName, client) {CommandType = CommandType.StoredProcedure};
            command.Parameters.AddRange(new []
            {
                new SqlParameter("StudiesId", studiesId),
                new SqlParameter("Semester", semester) 
            });

            command.ExecuteNonQuery();
            
            client.Close();
        }
    }
}