using DotnetLocationRest.Models;
using Microsoft.Data.SqlClient;

namespace DotnetLocationRest.Data
{
    public class ClientRepository
    {
        private readonly DbConnectionFactory _factory;

        public ClientRepository(DbConnectionFactory factory)
        {
            _factory = factory;
        }

        public List<Client> GetAll()
        {
            var clients = new List<Client>() ;

            using var conn = _factory.Create();
            conn.Open();

            var cmd = new SqlCommand("SELECT * FROM Clients", conn);
            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                clients.Add(new Client
                {
                    Id = (int)reader["Id"],
                    Nom = reader["Nom"].ToString(),
                    Prenom = reader["Prenom"].ToString()
                });
            }

            return clients;
        }

        public void Add(Client client)
        {
            using var conn = _factory.Create();
            conn.Open();

            var cmd = new SqlCommand(
                "INSERT INTO Clients (Nom, Prenom) VALUES (@Nom, @Prenom)", conn);

            cmd.Parameters.AddWithValue("@Nom", client.Nom);
            cmd.Parameters.AddWithValue("@Prenom", client.Prenom);

            cmd.ExecuteNonQuery();
        }

    }
}
