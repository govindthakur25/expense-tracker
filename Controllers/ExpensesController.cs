using ExpenseTracker.API.Helpers;
using ExpenseTracker.Repository;
using ExpenseTracker.Repository.Entities;
using ExpenseTracker.Repository.Factories;
using Marvin.JsonPatch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;

namespace ExpenseTracker.API.Controllers
{
    [RoutePrefix("api")]
    public class ExpensesController : ApiController
    {
        IExpenseTrackerRepository _repository;
        ExpenseFactory _expenseFactory = new ExpenseFactory();

        public ExpensesController()
        {
            _repository = new ExpenseTrackerEFRepository(new ExpenseTrackerContext());
        }

        public ExpensesController(IExpenseTrackerRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        [Route("expenses")]
        public IHttpActionResult Get()
        {
            try
            {
                var expenses = _repository.GetExpenses().ToList().Select(exp => _expenseFactory.CreateExpense(exp));

                if (expenses == null)
                {
                    return BadRequest();
                }
                return Ok(expenses);
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        [HttpGet]
        [Route("expensegroups/{expenseGroupId}/expenses")]
        public IHttpActionResult Get(int expenseGroupId, string fields = null)
        {
            try
            {
                List<string> lstFields = new List<string>();
                if (fields != null)
                {
                    lstFields = fields.ToLower().Split(',').ToList();
                }
                var expenses = _repository.GetExpenses(expenseGroupId);

                if (expenses == null)
                {
                    return NotFound();
                }
                var expensesResult = expenses.ToList().Select(exp => _expenseFactory.CreateDataShapedObject(exp, lstFields));
                return Ok(expensesResult);
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        [HttpGet]
        [VersionedRoute("expensegroups/{expenseGroupId}/expenses/{id}", 2)]
        [VersionedRoute("expenses/{id}", 2)]
        public IHttpActionResult Get(int id, int? expenseGroupId = null)
        {
            try
            {
                Expense expense = null;
                if (!expenseGroupId.HasValue)
                {
                    expense = _repository.GetExpense(id);
                }
                else
                {
                    var expensesForGroup = _repository.GetExpenses(expenseGroupId.Value);

                    if (expensesForGroup != null)
                    {
                        expense = expensesForGroup.FirstOrDefault(efg => efg.Id == id);
                    }
                }
                if (expense == null)
                {
                    return NotFound();
                }
                else
                {
                    var expensesResult = _expenseFactory.CreateExpense(expense);
                    return Ok(expensesResult);
                }
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        [Route("expenses/{id}")]
        public IHttpActionResult Delete(int id)
        {
            try
            {

                var result = _repository.DeleteExpense(id);

                if (result.Status == RepositoryActionStatus.Deleted)
                {
                    return StatusCode(HttpStatusCode.NoContent);
                }
                else if (result.Status == RepositoryActionStatus.NotFound)
                {
                    return NotFound();
                }

                return BadRequest();
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        [Route("expenses")]
        public IHttpActionResult Post([FromBody]DTO.Expense expense)
        {
            try
            {
                if (expense == null)
                {
                    return BadRequest();
                }

                // map
                var exp = _expenseFactory.CreateExpense(expense);

                var result = _repository.InsertExpense(exp);
                if (result.Status == RepositoryActionStatus.Created)
                {
                    // map to dto
                    var newExp = _expenseFactory.CreateExpense(result.Entity);
                    return Created<DTO.Expense>(Request.RequestUri + "/" + newExp.Id.ToString(), newExp);
                }

                return BadRequest();

            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }


        [Route("expenses/{id}")]
        public IHttpActionResult Put(int id, [FromBody]DTO.Expense expense)
        {
            try
            {
                if (expense == null)
                {
                    return BadRequest();
                }

                // map
                var exp = _expenseFactory.CreateExpense(expense);

                var result = _repository.UpdateExpense(exp);
                if (result.Status == RepositoryActionStatus.Updated)
                {
                    // map to dto
                    var updatedExpense = _expenseFactory.CreateExpense(result.Entity);
                    return Ok(updatedExpense);
                }
                else if (result.Status == RepositoryActionStatus.NotFound)
                {
                    return NotFound();
                }

                return BadRequest();
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }


        [Route("expenses/{id}")]
        [HttpPatch]
        public IHttpActionResult Patch(int id, [FromBody]JsonPatchDocument<DTO.Expense> expensePatchDocument)
        {
            try
            {
                // find 
                if (expensePatchDocument == null)
                {
                    return BadRequest();
                }

                var expense = _repository.GetExpense(id);
                if (expense == null)
                {
                    return NotFound();
                }

                //// map
                var exp = _expenseFactory.CreateExpense(expense);

                // apply changes to the DTO
                expensePatchDocument.ApplyTo(exp);

                // map the DTO with applied changes to the entity, & update
                var result = _repository.UpdateExpense(_expenseFactory.CreateExpense(exp));

                if (result.Status == RepositoryActionStatus.Updated)
                {
                    // map to dto
                    var updatedExpense = _expenseFactory.CreateExpense(result.Entity);
                    return Ok(updatedExpense);
                }

                return BadRequest();
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }
    }
}
