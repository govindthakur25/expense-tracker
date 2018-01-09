using ExpenseTracker.Repository;
using ExpenseTracker.Repository.Entities;
using ExpenseTracker.Repository.Factories;
using System;
using System.Linq;
using System.Web.Http;

namespace ExpenseTracker.API.Controllers
{
    public class ExpenseGroupStatusController : ApiController
    {
        IExpenseTrackerRepository _repository;
        ExpenseMasterDataFactory _expenseMasterDataFactory = new ExpenseMasterDataFactory();

        public ExpenseGroupStatusController()
        {
            _repository = new ExpenseTrackerEFRepository(new ExpenseTrackerContext());
        }

        public ExpenseGroupStatusController(IExpenseTrackerRepository repository)
        {
            _repository = repository;
        }

        public IHttpActionResult Get()
        {
            try
            {
                var egStatus = _repository.GetExpenseGroupStatusses().ToList()
                                    .Select(egs => _expenseMasterDataFactory.CreateExpenseGroupStatus(egs));

                return Ok(egStatus);
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }
    }
}
