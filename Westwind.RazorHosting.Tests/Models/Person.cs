using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Westwind.RazorHosting;
using System.IO;
using System.Web.Razor;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Text;

namespace RazorHostingTests
{
    [Serializable]
    public class Person
    {
        public string Name { get; set; }
        public string Company { get; set; }
        public Address Address { get; set; }
        public List<Address> Addresses {get; set;}        
        public DateTime Entered { get; set; }

        public Person()
        {
            Address = new Address();
            Addresses = new List<RazorHostingTests.Address>();
            Addresses.Add(new Address() { Street="32 Kaiea", City="Paia", Phone="324-1231"});
            Addresses.Add(new Address() {Street = "45 Null Ave", City = "Bullard", Phone = "121-1211"});
            
            Entered = DateTime.Now;            
        }
    }

    [Serializable]
    public class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string Phone { get; set; }
        public string Zip { get; set; }
    }
}
