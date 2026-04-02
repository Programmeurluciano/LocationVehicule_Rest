using Microsoft.Data.SqlClient;
using DotnetLocationRest.DTOs;
using BCrypt.Net;

namespace DotnetLocationRest.Data
{
    public class AuthRepository
    {
        private readonly DbConnectionFactory _dbFactory;

        public AuthRepository(DbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<ClientDto?> RegisterAsync(RegisterDto dto)
        {
            using var connection = _dbFactory.Create();
            await connection.OpenAsync();

            var checkEmailQuery = "SELECT COUNT(*) FROM Client WHERE Email = @Email";
            using (var checkCommand = new SqlCommand(checkEmailQuery, connection))
            {
                checkCommand.Parameters.Add(new SqlParameter("@Email", dto.Email));
                var count = (int)await checkCommand.ExecuteScalarAsync();

                if (count > 0)
                    throw new InvalidOperationException("Un compte avec cet email existe déjà");
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.MotDePasse);

            var insertQuery = @"
                INSERT INTO Client (Nom, Prenom, Email, Contact, MotDePasseHash)
                VALUES (@Nom, @Prenom, @Email, @Contact, @MotDePasseHash);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            int clientId;
            using (var command = new SqlCommand(insertQuery, connection))
            {
                command.Parameters.Add(new SqlParameter("@Nom", dto.Nom));
                command.Parameters.Add(new SqlParameter("@Prenom", dto.Prenom));
                command.Parameters.Add(new SqlParameter("@Email", dto.Email));
                command.Parameters.Add(new SqlParameter("@Contact", dto.Contact));
                command.Parameters.Add(new SqlParameter("@MotDePasseHash", passwordHash));

                clientId = (int)await command.ExecuteScalarAsync();
            }

            return new ClientDto
            {
                Id = clientId,
                Nom = dto.Nom,
                Prenom = dto.Prenom,
                Email = dto.Email,
                Contact = dto.Contact
            };
        }

        public async Task<ClientDto?> LoginAsync(LoginDto dto)
        {
            using var connection = _dbFactory.Create();
            await connection.OpenAsync();

            var query = @"
                SELECT Id, Nom, Prenom, Email, Contact, MotDePasseHash
                FROM Client
                WHERE Email = @Email";

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add(new SqlParameter("@Email", dto.Email));

            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            var passwordHash = reader.GetString(5);

            if (!BCrypt.Net.BCrypt.Verify(dto.MotDePasse, passwordHash))
                return null;

            return new ClientDto
            {
                Id = reader.GetInt32(0),
                Nom = reader.GetString(1),
                Prenom = reader.GetString(2),
                Email = reader.GetString(3),
                Contact = reader.GetString(4)
            };
        }

        public async Task<ClientDto?> GetClientByIdAsync(int id)
        {
            using var connection = _dbFactory.Create();
            await connection.OpenAsync();

            var query = @"
                SELECT Id, Nom, Prenom, Email, Contact
                FROM Client
                WHERE Id = @Id";

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add(new SqlParameter("@Id", id));

            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new ClientDto
            {
                Id = reader.GetInt32(0),
                Nom = reader.GetString(1),
                Prenom = reader.GetString(2),
                Email = reader.GetString(3),
                Contact = reader.GetString(4)
            };
        }
    }
}