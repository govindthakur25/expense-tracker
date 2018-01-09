using ExpenseTracker.API.Helpers;
using ExpenseTracker.Repository;
using ExpenseTracker.Repository.Entities;
using ExpenseTracker.Repository.Factories;
using Marvin.JsonPatch;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;
using System.Web.Http.Routing;

namespace ExpenseTracker.API.Controllers
{
    public class ExpenseGroupsController : ApiController
    {
        IExpenseTrackerRepository _repository;
        ExpenseGroupFactory _expenseGroupFactory = new ExpenseGroupFactory();
        const int maxPageSize = 5;
        public ExpenseGroupsController()
        {
            _repository = new ExpenseTrackerEFRepository(new 
                Repository.Entities.ExpenseTrackerContext());
        }

        public ExpenseGroupsController(IExpenseTrackerRepository repository)
        {
            _repository = repository;
        }

        [Route("api/expensegroups", Name = "ExpenseGroupsList")]
        public IHttpActionResult Get(string sort = "id", string status = null, string userId = null, string fields = null, int page = 1, int pageSize = 5)
        {
            try
            {
                int statusId = -1;
                if (!String.IsNullOrWhiteSpace(status))
                {
                    switch (status.ToLower())
                    {
                        case "open":
                            statusId = 1;
                            break;
                        case "confirmed":
                            statusId = 2;
                            break;
                        case "processed":
                            statusId = 3;
                            break;
                        default:
                            break;
                    }
                }

                bool includeExpenses = false;
                List<string> listOfFields = new List<string>();
                if (fields != null)
                {
                    listOfFields = fields.ToLower().Split(',').ToList();
                    includeExpenses = listOfFields.Any(f => f.Contains("expenses"));
                }
                IQueryable<ExpenseGroup> expenseGroups = null;

                expenseGroups = includeExpenses ? _repository.GetExpenseGroupsWithExpenses() : _repository.GetExpenseGroups();

                expenseGroups = expenseGroups.ApplySort(sort)
                                             .Where(eg => (statusId == -1 || eg.ExpenseGroupStatusId == statusId))
                                             .Where(eg => (userId == null || eg.UserId == userId));

                if (pageSize > maxPageSize)
                {
                    pageSize = maxPageSize;
                }
                int totalCount = expenseGroups.Count();
                int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                UrlHelper urlHelper = new UrlHelper(Request);

                string prevLink = page > 1 ? urlHelper.Link("ExpenseGroupsList", new
                                                                                {
                                                                                    page = page - 1,
                                                                                    pageSize = pageSize,
                                                                                    sort = sort,
                                                                                    fields = fields,
                                                                                    status = status,
                                                                                    userId = userId
                                                                                })
                                            : string.Empty;

                string nextLink = page < pageSize ? urlHelper.Link("ExpenseGroupsList", new
                                                                                {
                                                                                    page = page + 1,
                                                                                    pageSize = pageSize,
                                                                                    sort = sort,
                                                                                    fields = fields,
                                                                                    status = status,
                                                                                    userId = userId
                                                                                })
                                                  : "";

                var pagination = new
                {
                    currentPage = page,
                    pageSize = pageSize,
                    totalCount = totalCount,
                    totalPages = totalPages,
                    previousPageLink = prevLink,
                    nextPageLink = nextLink
                };

                HttpContext.Current.Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(pagination));

                var finalResult = expenseGroups
                        .Skip(pageSize * (page - 1))
                        .Take(pageSize)
                        .ToList()
                        .Select(eg => _expenseGroupFactory.CreateDataShapedObject(eg, listOfFields));

                return Ok(finalResult);

            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        [HttpGet]
        [Route("api/expensegroups/{id:int}")]
        public IHttpActionResult Get(int id)
        {
            try
            {
                var expenseGroup = _repository.GetExpenseGroup(id);

                if (expenseGroup == null)
                {
                    return NotFound();
                }

                return Ok(expenseGroup);
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        [HttpPost]
        public IHttpActionResult Post([FromBody] DTO.ExpenseGroup expenseGroup)
        {
            try
            {
                if (expenseGroup == null) return BadRequest();

                var eg = _expenseGroupFactory.CreateExpenseGroup(expenseGroup);
                var result = _repository.InsertExpenseGroup(eg);

                if (result.Status == RepositoryActionStatus.Created)
                {
                    var newExpenseGroup = _expenseGroupFactory.CreateExpenseGroup(result.Entity);

                    return Created(string.Format("{0}/{1}", Request.RequestUri, newExpenseGroup.Id), newExpenseGroup);
                }

                return BadRequest();
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        [HttpPatch]
        public IHttpActionResult Patch(int id, [FromBody]JsonPatchDocument<DTO.ExpenseGroup> jsonPatchDocument)
        {
            try
            {
                if (jsonPatchDocument == null)
                {
                    return BadRequest();
                }

                var expenseGroup = _repository.GetExpenseGroup(id);
                if (expenseGroup == null) return NotFound();

                var mappedEG = _expenseGroupFactory.CreateExpenseGroup(expenseGroup);
                jsonPatchDocument.ApplyTo(mappedEG);

                var newEG = _expenseGroupFactory.CreateExpenseGroup(mappedEG);
                var result = _repository.UpdateExpenseGroup(newEG);

                if (result.Status == RepositoryActionStatus.Updated)
                {
                    var patchedEG = _expenseGroupFactory.CreateExpenseGroup(result.Entity);
                    return Ok(patchedEG);
                }

                return BadRequest();
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        [HttpDelete]
        public IHttpActionResult Delete(int id)
        {
            try
            {
                var result = _repository.DeleteExpenseGroup(id);

                if(result.Status == RepositoryActionStatus.Deleted)
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

        /*
        private IEnumerable<DTO.ExpenseGroup> ApplyPaging(string sort, string status, string userId, List<string> listOfFields, int page, int pageSize, IQueryable<ExpenseGroup> expenseGroups)
        {
            if (pageSize > maxPageSize)
            {
                pageSize = maxPageSize;
            }
            int totalCount = expenseGroups.Count();
            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            UrlHelper urlHelper = new UrlHelper(Request);

            string prevLink = page > 1 ? urlHelper.Link("ExpenseGroupsList", new
                                                                            {
                                                                                page = page - 1,
                                                                                pageSize = pageSize,
                                                                                sort = sort,
                                                                                status = status,
                                                                                userId = userId
                                                                            }) 
                                        : string.Empty;

            string nextLink = page < pageSize ? urlHelper.Link("ExpenseGroupsList", new
                                                                                    {
                                                                                        page = page + 1,
                                                                                        pageSize = pageSize,
                                                                                        sort = sort,
                                                                                        status = status,
                                                                                        userId = userId
                                                                                    }) 
                                              : "";

            var pagination = new
            {
                currentPage = page,
                pageSize = pageSize,
                totalCount = totalCount,
                totalPages = totalPages,
                previousPageLink = prevLink,
                nextPageLink = nextLink
            };

            HttpContext.Current.Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(pagination));

            return expenseGroups
                    .Skip(pageSize * (page - 1))
                    .Take(pageSize)
                    .ToList()
                    .Select(eg => _expenseGroupFactory.CreateDataShapedObject(eg, listOfFields) as DTO.ExpenseGroup);
        }
*/
    }
}
