using Microsoft.Data.SqlClient;

namespace DotnetLocationRest.Data
{
    public class DbConnectionFactory
    {
        private readonly IConfiguration _configuration;

        public DbConnectionFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public SqlConnection Create()
        {
            return new SqlConnection(
                _configuration.GetConnectionString("DefaultConnection"));
        }
    }
}
