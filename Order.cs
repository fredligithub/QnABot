using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using System;
using System.ComponentModel;

namespace QnABot
{
    [Serializable]
    public class Order
    {
        public Product product;

        public Quanlity quantity;

        public DiskType disk;

        public Memory memory;

        public static IForm<Order> BuildForm()
        {
            return new FormBuilder<Order>()
             .Field(nameof(product))
             .Field(nameof(quantity))
             .Field(nameof(disk))
             .Field(nameof(memory))
            //.Message("Welcome to the Order page.")
            .OnCompletion(async (context, profileForm) =>
            {
                // Mail to 
                // Mail.SendMail();

                // Tell the user that the form is complete 
                await context.PostAsync("Thanks for your info. Your Order ( " + profileForm.quantity.ToString() + " " + profileForm.product
                    + " with " + profileForm.disk + " disk "
                    + " and " + profileForm.memory + " memory "
                    + " ) is completed!");
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
    public enum Quanlity
    {
        One =1, Two =2 , Three = 3, Four =4
    }

    [Serializable]
    public enum DiskType
    {
        HDD =1, SSD=2
    }

    [Serializable]
    public enum Memory
    {
        Standard_8GB = 1, Maximum_16GB = 2
    }

    
}