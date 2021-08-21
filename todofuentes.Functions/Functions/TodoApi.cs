using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using todofuentes.Common.Models;
using todofuentes.Common.Response;
using todofuentes.Functions.Entities;

namespace todofuentes.Functions.Functions
{
    public static class TodoApi
    {
        [FunctionName(nameof(CreateTodo))]
        public static async Task<IActionResult> CreateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "todo")] HttpRequest req,
            [Table("todo", Connection = "AzureWebJobsStorage")] CloudTable todoTable,
            ILogger log)
        {
            log.LogInformation("Recieved a new todo.");
            

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Todo todo = JsonConvert.DeserializeObject<Todo>(requestBody);

            if (string.IsNullOrEmpty(todo?.TaskDescription))
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "The request must have a TaskDescription."
                });
            }

            TodoEntity todoEntity = new TodoEntity
            {
                CreatedTime = DateTime.UtcNow,
                ETag = "*",
                IsCompleted = false,
                PartitionKey = "TODO",
                RowKey = Guid.NewGuid().ToString(),
                TaskDescription = todo.TaskDescription
            };

            TableOperation addOperation = TableOperation.Insert(todoEntity);
            await todoTable.ExecuteAsync(addOperation);

            string message = "New todo stored in table";
            log.LogInformation(message);


            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = todoEntity
            });
        }


        [FunctionName(nameof(UpdateTodo))]
        public static async Task<IActionResult> UpdateTodo(
          [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "todo/{id}")] HttpRequest req,
          [Table("todo", Connection = "AzureWebJobsStorage")] CloudTable todoTable,
          string id,
          ILogger log)
        {
            log.LogInformation($"Update for todo: {id}, Received.");

            //SE RECIBE LOS PARAMETROS
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Todo todo = JsonConvert.DeserializeObject<Todo>(requestBody);

            //VALIDATE TODO ID
            TableOperation findOperation = TableOperation.Retrieve<TodoEntity>("TODO", id);
            TableResult findResult = await todoTable.ExecuteAsync(findOperation);
            if(findResult.Result == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Todo not found."
                });

            }

            //UPDATE TODO
            TodoEntity todoEntity = (TodoEntity)findResult.Result;
            todoEntity.IsCompleted = todo.IsCompleted;
            if(!string.IsNullOrEmpty(todo.TaskDescription))
            {
                todoEntity.TaskDescription = todo.TaskDescription;
            }

            TableOperation addOperation = TableOperation.Replace(todoEntity);
            await todoTable.ExecuteAsync(addOperation);

            string message = $"Todo: {id}, updated in table";
            log.LogInformation(message);


            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = todoEntity
            });
        }


        [FunctionName(nameof(GetAllTodos))]
        public static async Task<IActionResult> GetAllTodos(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo")] HttpRequest req,
           //conexion a latabla
           [Table("todo", Connection = "AzureWebJobsStorage")] CloudTable todoTable,
           ILogger log)
        {   
            //se llama a la funcion
            log.LogInformation("Get all received.");

            //un objeto de tablequery
            TableQuery<TodoEntity> query = new TableQuery<TodoEntity>();

            //para filtrar la informacion todo los registros el segundo parametro es para cancelar si se demora
            TableQuerySegment<TodoEntity> todos = await todoTable.ExecuteQuerySegmentedAsync(query, null);


            string message = "Retreved all todos.";
            log.LogInformation(message);


            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = todos
            });
        }


        [FunctionName(nameof(GetTodoById))]
        public static IActionResult GetTodoById(
          [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo/{id}")] HttpRequest req,

          //conexion a latabla trae un todo de la tabla TODO Y AID A TRAER
          [Table("todo","TODO","{id}", Connection = "AzureWebJobsStorage")] TodoEntity todoEntity,
          string id,
          ILogger log)
        {
            //se llama a la funcion
            log.LogInformation($"Get todo by id: {id}, received.");

            //validar el id
            if (todoEntity == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Todo not found."
                });

            }

            string message =$"Todo: {id}, retrieved.";
            log.LogInformation(message);


            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = todoEntity
            });
        }


        [FunctionName(nameof(DeleteTodo))]
        public static async Task<IActionResult> DeleteTodo(
         [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "todo/{id}")] HttpRequest req,

         //conexion a latabla trae un todo de la tabla TODO Y AID A TRAER
         [Table("todo", "TODO", "{id}", Connection = "AzureWebJobsStorage")] TodoEntity todoEntity,
         //la conexion a la tabla
         [Table("todo", Connection = "AzureWebJobsStorage")] CloudTable todoTable,
         string id,
         ILogger log)
        {
            //se llama a la funcion
            log.LogInformation($"Delete todo: {id}, received.");

            //validar el objeto
            if (todoEntity == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Todo not found."
                });

            }

            //eliminar la tarea
            await todoTable.ExecuteAsync(TableOperation.Delete(todoEntity));

            string message = $"Todo: {todoEntity.RowKey}, deleted.";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = todoEntity
            });
        }

    }
}
