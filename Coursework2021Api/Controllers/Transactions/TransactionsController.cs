using System;
using System.Collections.Generic;
using System.Linq;
using Coursework2021DB.DB;
using Microsoft.AspNetCore.Mvc;

namespace Coursework2021Api.Controllers.Transactions
{
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        private readonly CourseDBContext context;

        public TransactionsController(CourseDBContext context)
        {
            this.context = context;
        }

        [HttpGet("/api/transactions")]
        public ActionResult<List<TransactionResponse>> Get()
        {
            var transactions = context.Transactions.Select(transaction => ResponseForModel(transaction))
                .ToList();
            return transactions;
        }

        [HttpGet("/api/transaction")]
        public ActionResult<TransactionResponse?> Get([FromQuery] string id)
        {
            var transaction = GetById(id);

            if (transaction == null) return BadRequest("No transaction found for id " + id);
            return ResponseForModel(transaction);
        }

        [HttpPut("/api/transaction")]
        public ActionResult<TransactionResponse> Put([FromBody] AddTransactionRequest request)
        {
            var rentalId = int.Parse(request.RentalId);
            var rental = context.RoomRentals.FirstOrDefault(r => r.Id == rentalId);
            if (rental == null) return BadRequest("Cannot find rental with id " + rentalId);

            var room = rental.Room;
            if (room == null) return BadRequest("Cannot find rental room with id " + rental.RoomId);

            var amount = room.PlacePrice * request.MonthsCount;

            var lastPaymentDate = rental.DatePaidUntil ?? rental.DateStart;
            var paidUntilDate = lastPaymentDate.AddMonths(request.MonthsCount);

            var transaction = CreateDBModel(request, amount, lastPaymentDate, paidUntilDate);
            context.Transactions.Add(transaction);
            SaveChanges(transaction);

            UpdateRentalPaidUntilDate(rental, paidUntilDate);

            return ResponseForModel(transaction);
        }

        [HttpDelete("/api/transaction")]
        public ActionResult Delete([FromQuery] string id)
        {
            var transaction = GetById(id);
            if (transaction == null) return BadRequest("Cannot find transaction by given id");

            context.Transactions.Remove(transaction);
            context.SaveChanges();

            return Ok();
        }

        private Transaction? GetById(string id)
        {
            var idInt = int.Parse(id);
            return context.Transactions.FirstOrDefault(t => t.Id == idInt);
        }

        private void SaveChanges(Transaction model)
        {
            context.SaveChanges();
            context.Entry(model).Reference(t => t.Manager).Load();
            context.Entry(model).Reference(t => t.Rent).Load();
        }

        private void UpdateRentalPaidUntilDate(RoomRental rental, DateTime paidUntil)
        {
            rental.DatePaidUntil = paidUntil;
            context.Update(rental);
            context.SaveChanges();
        }

        private static TransactionResponse ResponseForModel(Transaction transaction)
        {
            return new()
            {
                Id = transaction.Id.ToString(),
                RentalId = transaction.RentId.ToString(),
                UserId = transaction.Rent.UserId.ToString(),
                UserFirstName = transaction.Rent.User.FirstName,
                UserLastName = transaction.Rent.User.LastName,
                RoomId = transaction.Rent.RoomId.ToString(),
                RoomName = transaction.Rent.Room.Name,
                RoomType = transaction.Rent.Room.Type,
                ManagerId = transaction.ManagerId.ToString(),
                DateFrom = transaction.DatePaidFrom,
                DateTo = transaction.DatePaidTo,
                Amount = transaction.Amount,
                TimeCreated = transaction.TimeCreated
            };
        }

        private static Transaction CreateDBModel(
            AddTransactionRequest request,
            int amount,
            DateTime paidFrom,
            DateTime paidTo)
        {
            return new()
            {
                ManagerId = int.Parse(request.ManagerId),
                RentId = int.Parse(request.RentalId),
                Amount = amount,
                DatePaidFrom = paidFrom,
                DatePaidTo = paidTo,
                TimeCreated = DateTime.UtcNow
            };
        }
    }
}
