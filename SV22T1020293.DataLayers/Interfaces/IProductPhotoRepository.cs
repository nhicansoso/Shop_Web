using SV22T1020293.Models.Catalog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SV22T1020293.DataLayers.Interfaces
{
    public interface IProductPhotoRepository
    {
        Task<IList<ProductPhoto>> ListAsync(int productID);

        Task<long> AddAsync(ProductPhoto data);

        Task<bool> UpdateAsync(ProductPhoto data);

        Task<bool> DeleteAsync(long photoID);
    }
}