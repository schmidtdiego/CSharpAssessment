using System.Collections.Generic; 
using System.IO; 
using System.Linq;
using System.Runtime.CompilerServices;

namespace Viagogo
{
    public class Solution
    {
        private static IExternalAPI ExternalAPI { get; set; } = new ExternalAPI();
        public static void SetIExternalAPI(IExternalAPI iExternalAPI)
        {
            ExternalAPI = iExternalAPI;
        }
        public static void Cleanup()
        {
            CityDistances = new Dictionary<(string FromCity, string ToCity), int>();
        }

        const int MAX_EVENTS = 5;

        const int MAX_TRIES_API = 3;

        // TODO: ASK USER IF THIS VALUE IS CORRECT
        const int EXCEPTION_DISTANCE_VALUE = -1;


        static Dictionary<(string FromCity, string ToCity), int> CityDistances = new Dictionary<(string, string), int>();

        static void Main(string[] args)
        {
            Console.WriteLine("Starting...");

            var events = new List<Event>{
new Event{ Name = "Phantom of the Opera", City = "New York"},
new Event{ Name = "Metallica", City = "Los Angeles"},
new Event{ Name = "Metallica", City = "New York"},
new Event{ Name = "Metallica", City = "Boston"},
new Event{ Name = "LadyGaGa", City = "New York"},
new Event{ Name = "LadyGaGa", City = "Boston"},
new Event{ Name = "LadyGaGa", City = "Chicago"},
new Event{ Name = "LadyGaGa", City = "San Francisco"},
new Event{ Name = "LadyGaGa", City = "Washington"}
};
            //1. find out all events that arein cities of customer
            // then add to email.
            var customer = new Customer { Name = "Mr. Fake", City = "New York" };


            Task1(events, customer);
            Task2(events, customer);
            Task3(events, customer);
            Task4(events, customer);
            Task5(events, customer);
        }

        #region "Tasks"
        public static void Task1(List<Event> events, Customer customer)
        {
            Console.WriteLine("Task 1");
            events
                .Where(x => x.City == customer.City)
                .ToList()
                .ForEach(x => ExternalAPI.AddToEmail(customer, x));
        }
        public static void Task2(List<Event> events, Customer customer, int maxEvents = MAX_EVENTS)
        {
            Console.WriteLine("Task 2");
            events
                .OrderBy(x => ExternalAPI.GetDistance(customer.City, x.City))
                .Take(maxEvents)
                .ToList()
                .ForEach(x => ExternalAPI.AddToEmail(customer, x));
        }
        public static void Task3(List<Event> events, Customer customer, int maxEvents = MAX_EVENTS, int maxTriesApi = MAX_TRIES_API)
        {
            Console.WriteLine("Task 3");
            events
                .OrderBy(x => GetDistanceWithCacheAndRetry(customer.City, x.City, maxTriesApi))
                .Take(maxEvents)
                .ToList()
                .ForEach(x => ExternalAPI.AddToEmail(customer, x));
        }
        public static void Task4(List<Event> events, Customer customer, int maxEvents = MAX_EVENTS, int maxTriesApi = MAX_TRIES_API)
        {
            Console.WriteLine("Task 4");
            events
                .OrderBy(x => GetDistanceWithCacheAndRetry(customer.City, x.City, maxTriesApi, false))
                .Take(maxEvents)
                .ToList()
                .ForEach(x => ExternalAPI.AddToEmail(customer, x));
        }
        public static void Task5(List<Event> events, Customer customer, int maxEvents = MAX_EVENTS, int maxTriesApi = MAX_TRIES_API)
        {
            Console.WriteLine("Task 5");
            events
                .Select(x => (x, GetDistanceWithCacheAndRetry(customer.City, x.City, maxTriesApi, false), ExternalAPI.GetPrice(x)))
                .OrderBy(x => x.Item2)
                .ThenBy(x => x.Item3)
                .Take(maxEvents)
                .ToList()
                .ForEach(x => ExternalAPI.AddToEmail(customer, x.Item1, x.Item3));
        }
        #endregion "Tasks"

        private static int GetDistanceWithCacheAndRetry(string fromCity, string toCity, int maxTriesApi, bool throwException = true, int exceptionDistanceValue = EXCEPTION_DISTANCE_VALUE)
        {
            if (fromCity == toCity) 
                return 0;

            int distance;
            if (!CityDistances.TryGetValue((fromCity, toCity), out distance))
            {
                bool retry = maxTriesApi > 1;
                for (int i = 1; retry && i <= maxTriesApi; i++)
                {
                    try
                    {
                        distance = ExternalAPI.GetDistance(fromCity, toCity);
                        retry = false;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"GetDistance Exception in call {i} of {maxTriesApi}: {ex.Message}");
                        if (i == maxTriesApi)
                        {
                            if (throwException)
                                throw;
                            else
                                return exceptionDistanceValue;
                        }
                    }
                }

                CityDistances.Add((fromCity, toCity), distance);
                CityDistances.Add((toCity, fromCity), distance);
            }
                
            return distance;
        }

    }
    public class Event
    {
        public string Name { get; set; }
        public string City { get; set; }
    }

    public class Customer
    {
        public string Name { get; set; }
        public string City { get; set; }
    }

    public interface IExternalAPI
    {
        public void AddToEmail(Customer c, Event e, int? price = null);
        public int GetPrice(Event e);
        public int GetDistance(string fromCity, string toCity);
    }
    public class ExternalAPI : IExternalAPI
    {
        public void AddToEmail(Customer c, Event e, int? price = null)
        {
            ExternalAPIStatic.AddToEmail(c, e, price);
        }
        public int GetPrice(Event e)
        {
            return ExternalAPIStatic.GetPrice(e);
        }
        public int GetDistance(string fromCity, string toCity)
        {
            return ExternalAPIStatic.GetDistance(fromCity, toCity);
        }

    }
    public class ExternalAPIStatic
    {
        // You do not need to know how these methods work
        public static void AddToEmail(Customer c, Event e, int? price = null)
        {
            var distance = GetDistance(c.City, e.City);
            Console.Out.WriteLine($"{c.Name}: {e.Name} in {e.City}"
            + (distance > 0 ? $" ({distance} miles away)" : "")
            + (price.HasValue ? $" for ${price}" : ""));
        }
        public static int GetPrice(Event e)
        {
            return (AlphebiticalDistance(e.City, "") + AlphebiticalDistance(e.Name, "")) / 10;
        }
        public static int GetDistance(string fromCity, string toCity)
        {
            return AlphebiticalDistance(fromCity, toCity);
        }
        private static int AlphebiticalDistance(string s, string t)
        {
            var result = 0;
            var i = 0;
            for (i = 0; i < Math.Min(s.Length, t.Length); i++)
            {
                // Console.Out.WriteLine($"loop 1 i={i} {s.Length} {t.Length}");
                result += Math.Abs(s[i] - t[i]);
            }
            for (; i < Math.Max(s.Length, t.Length); i++)
            {
                // Console.Out.WriteLine($"loop 2 i={i} {s.Length} {t.Length}");
                result += s.Length > t.Length ? s[i] : t[i];
            }
            return result;
        }
    }

}