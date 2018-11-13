using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using System;

namespace QnABot
{
    [Serializable]
    public class Order
    {
        public Product product;

        [Numeric(1, 5)]
        public int? quantity; // type: Integral  

        public Memory memory;
        public static IForm<Order> BuildForm()
        {
            return new FormBuilder<Order>()
             .Field(nameof(product))
             .Field(nameof(quantity))
             .Field(nameof(memory))
            //.Message("Welcome to the Order page.")
            .OnCompletion(async (context, profileForm) =>
            {
                // Tell the user that the form is complete 
                await context.PostAsync("Thanks for your info. Your Order ( " + profileForm.quantity.ToString() + " " + profileForm.product 
                    + " with " + profileForm.memory
                    + " )is successfull");
            })
            .Build();
        }
    }

    [Serializable]
    public enum Product
    {
        DELL = 1, Lenovo = 2, SONY = 3
    }
    [Serializable]
    public enum Memory
    {
        FourGB = 1, EightGB = 2
    }
}