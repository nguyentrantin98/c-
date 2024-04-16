using c_.Models;

namespace c_.Controller
{
    public class ProductController : GenericController<Product>
    {
        public ProductController(IConfiguration configuration) : base(configuration)
        {
        }
    }
}
