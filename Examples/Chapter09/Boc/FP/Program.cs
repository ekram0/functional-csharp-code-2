﻿using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

using Boc.Commands;
using Boc.Domain;

using LaYumba.Functional;
using Unit = System.ValueTuple;

using Examples;

using Microsoft.AspNetCore.Http;
using static Microsoft.AspNetCore.Http.Results;

namespace Boc.Chapter9
{
   public static class Program
   {
      public async static Task Run()
      {
         var app = WebApplication.Create();
         Func<MakeTransfer, IResult> handleSaveTransfer = ConfigureSaveTransferHandler(app.Configuration);

         app.MapPost("/Transfer/Future", handleSaveTransfer);

         await app.RunAsync();
      }

      static Func<MakeTransfer, IResult>
         ConfigureSaveTransferHandler(IConfiguration config)
      {
         // persistence layer
         ConnectionString connString = config.GetSection("ConnectionString").Value;
         SqlTemplate InsertTransferSql = "INSERT ...";

         var save = connString.TryExecute(InsertTransferSql);

         var validate = Validation.DateNotPast(clock: () => DateTime.UtcNow);

         return HandleSaveTransfer(validate, save);
      }

      static Func<MakeTransfer, IResult> HandleSaveTransfer
         ( Validator<MakeTransfer> validate
         , Func<MakeTransfer, Exceptional<Unit>> save)
         => transfer
         => validate(transfer).Map(save).Match
            (
               Invalid: err => BadRequest(err),
               Valid: result => result.Match
               (
                  Exception: _ => InternalServerError(Errors.UnexpectedError),
                  Success: _ => Ok()
               )
            );
   }
}
