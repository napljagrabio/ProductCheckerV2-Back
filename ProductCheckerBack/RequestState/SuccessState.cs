using ProductCheckerBack.ProductCheckerState;
using ProductCheckerBack.RequestState.SuccessStateHandler;

namespace ProductCheckerBack.RequestState
{
    internal class SuccessState : IRequestState
    {
        private readonly ProductCheckerDbContext _productCheckerDbContext;

        public SuccessState(ProductCheckerDbContext productCheckerDbContext)
        {
            _productCheckerDbContext = productCheckerDbContext;
        }

        public void Process(ProductCheckerService productCheckerService)
        {
            productCheckerService.MarkAsProcessing();

            HandlerProcessor successHandlers = new HandlerProcessor()
            {
                typeof(CheckProductAvailability)
            };
            successHandlers.Process(_productCheckerDbContext, productCheckerService, new List<string>());
        }
    }
}