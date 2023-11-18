using System;
using System.Diagnostics;
using System.Globalization;
using System.IO.Enumeration;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.FileIO;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ISDTApp.Application
{
    enum ExitCode : int
    {
        Success = 0,
        BadFilePath = 1,
        FailedParse = 2,
        BadTimeStamps = 3,
        UnknownError = 4
    }

    public class Parser
    {
        public int DoWork(string csv_file_path)
        {

            if (!File.Exists(csv_file_path))
            {
                Console.WriteLine($"File with path '{csv_file_path}' does not exist!");
                return (int)ExitCode.BadFilePath;
            }

            /*
             * ============
             * 
             * DECLARATIONS
             * 
             * ============
             */

            int running_time = 0; // tracks running time in seconds
            int faulted_time = 0; // tracks faulted time in seconds
            int total_rows = 0; // tracks all rows in csv
            bool run_state = false; // current run state of our machine
            DateTime d_prev = new(2020, 08, 19); // previous state time (preloaded default)
            DateTime d_next = new(2020, 08, 19); // current state time (preloaded default)
            List<Tuple<int, int>> alarm_codes = new(); // variable length list of alarm codes
            double availability_percent = 0.0; // tracks available uptime
            bool alarm_code_state = false; // tracks prior row alarm state for duration calculations
            int prior_row_alarm_code = 0; // tracks prior row alarm code for duration calculations

            /*
             * =============
             * 
             * FILE HANDLING
             * 
             * =============
             */

            // parse csv
            using (TextFieldParser parser = new(csv_file_path))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                while (!parser.EndOfData)
                {
                    string[] fields = parser.ReadFields();

                    // range step as we know there are 3 columns per row
                    for (int i = 0; i < fields.Length; i += 3)
                    {
                        try
                        {
                            if (total_rows == 1) // setup row
                            {
                                // populate first run values

                                d_prev = DateTime.Parse(fields[i + 1], CultureInfo.InvariantCulture); // store first date value

                                run_state = fields[i] == "running"; // store first state value
                            }
                            else if (total_rows != 0) // skipped header row and setup row, process everything else
                            {
                                /* 
                                 * i is state
                                 * i+1 is timestamp
                                 * i+2 is alarm code
                                 */

                                d_next = DateTime.Parse(fields[i + 1], CultureInfo.InvariantCulture); // obtain this state timestamp for calculations
                                var diff = (int)(d_next - d_prev).TotalSeconds; // calulate time diff between prior and this state
                                if (diff < 0)
                                {
                                    throw new System.ArithmeticException("Timestamps are out of order!");
                                }
                                //add to correct cumulative
                                if (alarm_code_state) { alarm_codes.Add(new Tuple<int, int>(prior_row_alarm_code, diff)); } // check alarm code from prior state and add diff if needed
                                if (run_state) { running_time += diff; } // running state diff addition
                                else { faulted_time += diff; } // faulted state diff addition
                                d_prev = d_next; // update our timestamp

                                run_state = fields[i] == "running"; // update row state
 
                                alarm_code_state = Int32.TryParse(fields[i + 2], out prior_row_alarm_code); // check this state for int error code
                                if (!string.IsNullOrEmpty(fields[i + 2]) && !alarm_code_state)
                                {
                                    throw new System.FormatException("Bad alarm code (non numeric/null)!");
                                }

                                    
                            }
                            total_rows += 1; // increment processed rows
                        }
                        catch (Exception ex) // in case there's an exception while processing
                        {
                            if (ex is System.FormatException) // known quantity, datatypes couldn't be parsed or were unexpected values
                            {
                                Console.WriteLine($"There was an error with input data on row {total_rows + 1}. {ex.Message}");
                                return (int)ExitCode.FailedParse;
                            }
                            else if (ex is System.ArithmeticException)
                            {
                                Console.WriteLine($"There was an error with timestamps on row {total_rows + 1}. {ex.Message}");
                                return (int)ExitCode.BadTimeStamps;
                            }
                            else
                            { // catch all, would need much more testing to ensure targeted messages
                                Console.WriteLine($"Unknown exception caught: {ex.Message}");
                                return (int)ExitCode.UnknownError;
                            }
                        }
                    }
                }

            }

            /*
             * =====================
             * 
             * FLOATING CALCULATIONS
             * 
             * =====================
             */

            availability_percent = (double)running_time / (running_time + faulted_time); // calculate our available uptime % which is total uptime divided by total runtime

            // combine alarm code data
            var grouped_alarm_codes = alarm_codes.ToLookup(x => x.Item1) // get all first tuple items as 'keys'
                .Select(x => (key: x.Key, sum: x.Select(tuple => tuple.Item2).Sum())) // sum all matching 'keys'
                .OrderByDescending(x => x.sum) // sort by the sum
                .ToList().Take(5); // get the list, take top 5

            /*
             * =============
             * 
             * RESULTS BLOCK
             * 
             * =============
             */


            //Console.WriteLine($"Processed {total_rows} rows.");
            Console.WriteLine($"Running time: {running_time} seconds");
            Console.WriteLine($"Faulted time: {faulted_time} seconds");
            Console.WriteLine($"Availability: {availability_percent:P}");
            Console.WriteLine("Top 5 Alarm Codes (Duration in seconds):");
            foreach (var (key, sum) in grouped_alarm_codes)
            {
                Console.WriteLine($" - {key} ({sum})");
            }

            return (int)ExitCode.Success;
        }
    }

    public class Program
    {

        public static int Main(string[] args)
        {

            /*
             * ===================
             * 
             * ARGUMENT MANAGEMENT
             * 
             * ===================
             */

            string file_name = "MachineStateLog"; // in case the file name changes, for default path

            string csv_file;
            if (args.Length == 0 ) // check value was passed
            {
                csv_file = $@"X:\isdt\{file_name}.csv"; // default path state, change as required
            }
            else
            {
                csv_file = args[0]; // argument path state
            }

            Parser parser = new();

            // store the value in case we wish to do further processing at some point on anything but success state
            int return_value = parser.DoWork(csv_file);

            // send out a termination message on anything but success
            if (return_value != 0)
            {
                Console.WriteLine("\nTerminating Application...");
            }

            return return_value;
        }
    }
}