using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020293.DataLayers.Interfaces;
using SV22T1020293.Models.DataDictionary;

namespace SV22T1020293.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho tỉnh/thành
    /// </summary>
    public class ProvinceRepository : IDataDictionaryRepository<Province>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối CSDL</param>
        public ProvinceRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Lấy danh sách tỉnh/thành
        /// </summary>
        /// <returns></returns>
        public async Task<List<Province>> ListAsync()
        {
            using var connection = new SqlConnection(_connectionString);

            var data = await connection.QueryAsync<Province>(
                @"SELECT ProvinceName
                  FROM Provinces
                  ORDER BY ProvinceName");

            return data.ToList();
        }
    }
}