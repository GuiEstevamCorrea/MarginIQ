using Domain.Entities;
using Domain.Enums;

namespace Domain.Repositories;

/// <summary>
/// Repository interface for Product entity operations
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Gets a product by its unique identifier
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product if found, null otherwise</returns>
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a product by its SKU for a specific company
    /// </summary>
    /// <param name="sku">Product SKU</param>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product if found, null otherwise</returns>
    Task<Product?> GetBySkuAsync(string sku, Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all products for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of products</returns>
    Task<IEnumerable<Product>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active products for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active products</returns>
    Task<IEnumerable<Product>> GetActiveByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets products by category for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="category">Product category</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of products in the specified category</returns>
    Task<IEnumerable<Product>> GetByCategoryAsync(Guid companyId, string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets products by status for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="status">Product status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of products with the specified status</returns>
    Task<IEnumerable<Product>> GetByStatusAsync(Guid companyId, ProductStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches products by name for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="searchTerm">Search term to match against product names</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of products matching the search term</returns>
    Task<IEnumerable<Product>> SearchByNameAsync(Guid companyId, string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all distinct categories for a company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of distinct category names</returns>
    Task<IEnumerable<string>> GetCategoriesAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new product to the repository
    /// </summary>
    /// <param name="product">Product to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing product
    /// </summary>
    /// <param name="product">Product to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a product exists by ID
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if product exists, false otherwise</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a product with the given name already exists in a company
    /// </summary>
    /// <param name="name">Product name</param>
    /// <param name="companyId">Company ID</param>
    /// <param name="excludeId">Optional product ID to exclude from the check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if a product with the name exists in the company, false otherwise</returns>
    Task<bool> ExistsByNameAsync(string name, Guid companyId, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a product with the given SKU already exists in a company
    /// </summary>
    /// <param name="sku">Product SKU</param>
    /// <param name="companyId">Company ID</param>
    /// <param name="excludeId">Optional product ID to exclude from the check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if a product with the SKU exists in the company, false otherwise</returns>
    Task<bool> ExistsBySkuAsync(string sku, Guid companyId, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
