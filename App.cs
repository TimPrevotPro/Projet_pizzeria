﻿using System;
using System.Data;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Schema;
using Npgsql;

namespace Projet_pizzeria
{
    public class App
    {
        public App()
        {
            this.actualClient = new User();
            this.connectedClerk = new User();
        }

        private User connectedClerk;
        private User actualClient;
        
        public User ConnectedClerk { get; set; }
        
        public User ActualClient { get; set; }
        
        public bool IsConnected { get; set; }
        
        // Function to connect the clerk
        public bool connectClerk()
        {
            const string postgreConStr = "Server=localhost;Port=5432;UserId=postgres;Password=abcd1234;Database=Projet_pizzeria;";
            var ncon = new NpgsqlConnection(postgreConStr);
            
            Console.WriteLine("username: ");
            var username = Console.ReadLine();
            Console.WriteLine("password: ");
            var hashedPwd = "";
            var user_id = -1;
            var firstname = "";
            var lastname = "";
            var telephone = "";
            var pwd = Console.ReadLine();
            var cmd = new NpgsqlCommand("SELECT * FROM users WHERE username = '" + username + "'", ncon);
            Console.WriteLine("SELECT * FROM users WHERE username = '" + username + "'");
            ncon.Open();
            var dr = cmd.ExecuteReader();

            var dt = new DataTable();
            dt.Columns.Add(new DataColumn("user_id", typeof(int)));
            dt.Columns.Add(new DataColumn("firstname", typeof(string)));
            dt.Columns.Add(new DataColumn("lastname", typeof(string)));
            dt.Columns.Add(new DataColumn("telephone", typeof(string)));
            dt.Columns.Add(new DataColumn("username", typeof(string)));
            dt.Columns.Add(new DataColumn("password", typeof(string)));
            while (dr.Read())
            {
                var row = dt.NewRow();
                row["user_id"] = dr["user_id"];
                row["firstname"] = dr["firstname"];
                row["lastname"] = dr["lastname"];
                row["telephone"] = dr["telephone"];
                row["password"] = dr["password"];
                dt.Rows.Add(row);
                hashedPwd = dr["password"].ToString();
                user_id = Convert.ToInt32(dr["user_id"].ToString());
                firstname = dr["firstname"].ToString();
                lastname = dr["lastname"].ToString();
                telephone = dr["telephone"].ToString();
            }
            ncon.Close();
            dt.AcceptChanges();
            
            using (SHA256 sha256Hash = SHA256.Create())
            {
                string hash = GetHash(sha256Hash, pwd);
                Console.WriteLine("Verifying password...");
                if (VerifyHash(sha256Hash, pwd, hash))
                {
                    Console.WriteLine("Authentication successful.");
                    connectedClerk.UserId = user_id;
                    connectedClerk.FirstName = firstname;
                    connectedClerk.LastName = lastname;
                    connectedClerk.Tel = telephone;
                    connectedClerk.Username = username;
                    
                    Console.WriteLine("Clerk connected : " + connectedClerk.FirstName + " " +
                                      connectedClerk.LastName + ", tel: " + connectedClerk.Tel);
                    return true;

                }
                else
                {
                    Console.WriteLine("Authentication failed");
                    return false;
                }
            }
        }

        private static string GetHash(HashAlgorithm hashAlgorithm, string input)
        {
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return sBuilder.ToString();
        }

        private static bool VerifyHash(HashAlgorithm hashAlgorithm, string input, string hash)
        {
            var hashOfInput = GetHash(hashAlgorithm, input);
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            return comparer.Compare(hashOfInput, hash) == 0;
        }

        // Function to get the client from the db if this isn't his first order, or create him and add him in the db instead
        public void checkUser()
        {
            Console.WriteLine("Is this the client's first order ? Type 1 for Yes, 0 for No :");
            var isNew = Convert.ToInt32(Console.ReadLine());
            while (isNew != 0 && isNew != 1)
            {
                Console.WriteLine("Please enter a correct value.");
                isNew = Convert.ToInt32(Console.ReadLine());
            }

            // If this is not the client's first order, we get his info from the database
            if (isNew == 1)
            {
                const string postgresConStr =
                    "Server=localhost;Port=5432;UserId=postgres;Password=abcd1234;Database=Projet_pizzeria;";
                var ncon = new NpgsqlConnection(postgresConStr);
                Console.WriteLine("Please enter the client's phone number");
                var telClient = Console.ReadLine();
                var cmd = new NpgsqlCommand("SELECT * FROM users WHERE telephone = '" + telClient + "'", ncon);
                ncon.Open();
                var dr = cmd.ExecuteReader();

                var dt = new DataTable();
                dt.Columns.Add(new DataColumn("user_id", typeof(int)));
                dt.Columns.Add(new DataColumn("firstname", typeof(string)));
                dt.Columns.Add(new DataColumn("lastname", typeof(string)));
                dt.Columns.Add(new DataColumn("telephone", typeof(string)));
                dt.Columns.Add(new DataColumn("address", typeof(string)));
                dt.Columns.Add(new DataColumn("city", typeof(string)));
                dt.Columns.Add(new DataColumn("postal_code", typeof(string)));
                dt.Columns.Add(new DataColumn("entity_id", typeof(int)));
                while (dr.Read())
                {
                    var row = dt.NewRow();
                    row["user_id"] = dr["user_id"];
                    row["firstname"] = dr["firstname"];
                    row["lastname"] = dr["lastname"];
                    row["telephone"] = dr["telephone"];
                    row["address"] = dr["address"];
                    row["city"] = dr["city"];
                    row["postal_code"] = dr["postal_code"];
                    row["entity_id"] = dr["entity_id"];
                    dt.Rows.Add(row);
                    this.actualClient.UserId = Convert.ToInt32(dr["user_id"].ToString());
                    this.actualClient.FirstName = dr["firstname"].ToString();
                    this.actualClient.LastName = dr["lastname"].ToString();
                    this.actualClient.Tel = dr["telephone"].ToString();
                    this.actualClient.Address = dr["address"].ToString();
                    this.actualClient.City = dr["city"].ToString();
                    this.actualClient.PostalCode = dr["postal_code"].ToString();
                    this.actualClient.Entity = Convert.ToInt32(dr["entity_id"].ToString());
                }

                ncon.Close();
                dt.AcceptChanges();
                Console.WriteLine("Client : " + this.actualClient.FirstName + " " +
                                  this.actualClient.LastName + ", tel: " + this.actualClient.Tel);
            }
            else if (isNew == 0)
            {
                this.addNewUser();
            }
        }

        public void addNewUser()
        {
            Console.WriteLine("Please enter the user's first name: ");
            var firstname = Console.ReadLine();
            Console.WriteLine("Please enter the user's last name: ");
            var lastname = Console.ReadLine();
            Console.WriteLine("Please enter the user's phone number with the indicative: ");
            var tel = Console.ReadLine();
            Console.WriteLine("Please enter the user's address: ");
            var address = Console.ReadLine();
            Console.WriteLine("Please enter the user's city: ");
            var city = Console.ReadLine();
            Console.WriteLine("Please enter the user's postal code: ");
            var postalCode = Console.ReadLine();
            Console.WriteLine("Please enter the user's entity type: ");
            Console.WriteLine("1: clerk");
            Console.WriteLine("2: client");
            var entity = Convert.ToInt32(Console.ReadLine());
            string username = null;
            string pwd = null;
            if (entity == 1)
            {
                Console.WriteLine("Please enter the user's username: ");
                username = Console.ReadLine();
                Console.WriteLine("Please enter the user's password: ");
                pwd = Console.ReadLine();
                using (SHA256 sha256hash = SHA256.Create())
                {
                    var hashPwd = GetHash(sha256hash, pwd);
                    pwd = hashPwd;
                }
            }

            const string postgreConStr =
                "Server=localhost;Port=5432;UserId=postgres;Password=abcd1234;Database=Projet_pizzeria;";
            var ncon = new NpgsqlConnection(postgreConStr);
            var req = "";
            if (entity == 1)
            {
                req = "INSERT INTO users VALUES (default, '" + firstname + "', '" + lastname + "', '" + tel + "', '" + address +
                          "', '" + city + "', '" + postalCode + "', '" + entity + "', '" + username + "', '" + pwd + "')";
            }
            else
            {
                req = "INSERT INTO users VALUES (default, '" + firstname + "', '" + lastname + "', '" + tel + "', '" + address +
                          "', '" + city + "', '" + postalCode + "', '" + entity + "')";
            }
            
            Console.WriteLine("req: " + req);
            var cmd = new NpgsqlCommand(req, ncon);
            ncon.Open();
            cmd.ExecuteNonQuery();
            Console.WriteLine("Client added !");
        }

        public void createOrder()
        {
            var newOrder = new Order();
            newOrder.Date = DateTime.Now;
            
            Console.WriteLine("Created order");
        }

        public static void Main(string[] args)
        {
            var myApp = new App();
            bool closeApp = false;
            bool isConnected = false;
            while (!closeApp)
            {
                while (!isConnected)
                {
                    isConnected = myApp.connectClerk();
                }
                Console.WriteLine("To create a new order, type 1");
                Console.WriteLine("To add a new user, type 2");
                Console.WriteLine("To exit the app, type 0");
                var choice = Convert.ToInt32(Console.ReadLine());
                while (choice != 1 && choice != 2 && choice != 0)
                {
                    Console.WriteLine("Please enter a correct value.");
                    choice = Convert.ToInt32(Console.ReadLine());
                }

                if (choice == 1)
                {
                    myApp.checkUser();
                    myApp.createOrder();
                }
                else if (choice == 2)
                {
                    myApp.addNewUser();
                }
                else if (choice == 0)
                {
                    closeApp = true;
                }
            }
        }

        static void PrintTable(DataTable dt)
        {
            foreach (DataColumn dc in dt.Columns)
            {
                Console.Write(dc.ColumnName + "   ");
            }

            Console.WriteLine();
            Console.WriteLine();
            foreach (DataRow dr in dt.Rows)
            {
                for (int i = 0; i < dr.ItemArray.Length; i++)
                {
                    Console.Write(dr[i] + "\t");
                }

                Console.WriteLine();
                Console.WriteLine();
            }
        }
    }
}