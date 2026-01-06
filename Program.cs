/***************************************************************************************
 *                  TableFill program for TaskSchedulerOneTimeSealevel                 *
 **************************************************************************************/
/*******************************************************************************************************************************************************
*   All Project Files Copyright © 2025, 2026 by The ep5 Educational Broadcasting Foundation. All rights reserved.                                      *
*                                                                                                                                                      *
*   Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the “Software”)  *
*	to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, *
*	and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:         *
*                                                                                                                                                      *
*        →  Redistributions of source code must retain the above copyright notice, this list of conditions, and the following disclaimer:              *
*        →  Redistributions in binary form must reproduce the above copyright notice, this list of conditions, and the following disclaimer in the     *
*           documentation and/or other materials provided with the distribution.                                                                       *
*        →  Neither the name of the copyright holder nor the names of its contributors may be used to endorse or promote products derived from this    *
*           software without specific prior written permission.                                                                                        *
*                                                                                                                                                      *
*    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS “AS IS” AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED     *
*    TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO  EVENT SHALL THE COPYRIGHT HOLDER OR     *
*    CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,         *
*    PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF         *
*    LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT, INCLUDING NEGLIGENCE OR OTHERWISE, ARISING IN ANY WAY FROM THE USE OF THIS             *
*    SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.                                                                                      *
*******************************************************************************************************************************************************/

//  Written/updated 6 January 2026 by David Fisher
//  Copyright © 2025, 2026 by The ep5 Educational Broadcasting Foundation; all rights reserved


using Microsoft.Data.SqlClient;
using static System.Console;

namespace TableFill
{
    /// <summary>
    ///     This program creates a series of events to be executed by another program that reads from the ControlEvent table.
    ///     It is intended to populate the table with test data for development and debugging purposes. Further, the date and time values
    ///         are set to be relative to the current date and time so that the events begin promptly when the other program is run.
    /// </summary>
    /// <to-do>
    ///     Change database program from MS SQL Server to PostgreSQL or SQLite for better cross-platform compatibility.
    ///     Consider adding error handling for database operations and I/O operations to enhance robustness.
    ///     Consider implementing logging to track the execution of events and any errors that occur.
    ///     Consider adding configuration options to specify the database connection string and recipe ID.
    ///     Consider implementing unit tests for the methods to ensure correctness.
    ///     Consider restructuring the program to fit the ep5BAS modular design pattern for better maintainability and adaptability to other I/O processors.
    /// </to-do>
    class Program
    {     
        static void Main()
        {
            int channelNmbr;        
            int recipeID = 1;
            DateTime startingTime;

            SqlConnection connection = new("Server = System76; Database = TaskSchedulerOneTimeSealevel; Integrated Security = SSPI; TrustServerCertificate = true");
            connection.Open();
            string truncateQuery = "truncate table dbo.ControlEvent;";
            SqlCommand truncateCommand = new(truncateQuery, connection);
            truncateCommand.ExecuteNonQuery();
            string query = "INSERT INTO dbo.ControlEvent (recipeID, channelNmbr, doAt, whenEntered, whenEdited, enteredBy, editedBy, digitalValue, notes, status) " +
                "VALUES(@recipeID, @channelNmbr, @doAt, @whenEntered, @whenEdited, @enteredBy, @editedBy, @digitalValue, @notes, @status)";
            SqlCommand command = new(query, connection);
            channelNmbr = 0;
            startingTime = EnterSequentialControlEvents(recipeID, channelNmbr, command);
            startingTime = TurnAllOutputsOff(recipeID, command, startingTime);
            _ = EnterRandomControlEvents(recipeID, channelNmbr, command, startingTime);
            connection.Close();
            WriteLine("All entries have been written to the database.\n");
            WriteLine("We're done here, Sparky...\nHave a nice day . . . somewhere else.");
            return;
        }

        /// <remarks>
        ///     This enables the program to be tested immediately with current usable recipe data.
        ///     It creates a series of ON/OFF events for channels 0 through 23, with each channel being toggled twice in succession.
        ///     The effect is a parade of LEDs lighting up in numerical succession, demonstrating that each output can be written to in a predictable and controlled manner.
        /// </remarks>
        static private DateTime EnterSequentialControlEvents(int recipeID, int channelNmbr, SqlCommand command)
        {
            //byte doDayOfWeek = (byte)DateTime.Now.DayOfWeek;
            TimeSpan timeToAdd = TimeSpan.FromSeconds(1);
            DateTime doAt = new();
            doAt = (DateTime.Now).Add(timeToAdd);
            DateTime whenEntered;
            DateTime whenEdited;
            TimeSpan hopTime = TimeSpan.FromMilliseconds(80);
            int enteredBy = 1;
            int editedBy = 1;
            byte digitalValue;
            string notes;
            byte status = 1;

            whenEntered = whenEdited = DateTime.Now;
            digitalValue = 1;
            notes = "Sequential test entry";

            command.Parameters.AddWithValue("@recipeID", recipeID);
            command.Parameters.AddWithValue("@channelNmbr", channelNmbr);
            command.Parameters.AddWithValue("@doAt", doAt);
            command.Parameters.AddWithValue("@whenEntered", whenEntered);
            command.Parameters.AddWithValue("@whenEdited", whenEdited);
            command.Parameters.AddWithValue("@enteredBy", enteredBy);
            command.Parameters.AddWithValue("@editedBy", editedBy);
            command.Parameters.AddWithValue("@digitalValue", digitalValue);
            command.Parameters.AddWithValue("@notes", notes);
            command.Parameters.AddWithValue("@status", status);
            command.ExecuteNonQuery();

            for (int indx = 1; indx < 48; ++indx)
            {
                //  Index channel number up after two writes
                if (indx % 2 == 0)
                    command.Parameters["@channelNmbr"].Value = ++channelNmbr;
                //  Flip channel output state
                switch (digitalValue)
                {
                    case 1: digitalValue = 0; break;
                    case 0: digitalValue = 1; break;
                }
                command.Parameters["@digitalValue"].Value = digitalValue;
                //  Increment op-time
                doAt = doAt.Add(hopTime);
                command.Parameters["@doAt"].Value = doAt;
                command.Parameters["@recipeID"].Value = recipeID;
                command.ExecuteNonQuery();
            }
            for (int indx = 47; indx > 0; --indx)
            {
                //  Index channel number up after two writes
                if (indx % 2 == 0)
                    command.Parameters["@channelNmbr"].Value = --channelNmbr;
                //  Flip channel output state
                switch (digitalValue)
                {
                    case 1: digitalValue = 0; break;
                    case 0: digitalValue = 1; break;
                }
                command.Parameters["@digitalValue"].Value = digitalValue;
                //  Increment op-time
                doAt = doAt.Add(hopTime);
                command.Parameters["@doAt"].Value = doAt;
                command.Parameters["@recipeID"].Value = recipeID;
                command.ExecuteNonQuery();
            }
            return doAt;
        }

        /// <summary>
        ///     This turns all digital outputs off, as quickly as possible, before the random test entries begin.
        /// </summary>
        static private DateTime TurnAllOutputsOff(int recipeID, SqlCommand command, DateTime startAt)
        {
            DateTime whenEntered = DateTime.Now;
            DateTime whenEdited = DateTime.Now;
            startAt = startAt.AddMilliseconds(100);
            int enteredBy = 1;
            int editedBy = 1;
            byte digitalValue = 0;
            string notes = "Turn all digital outputs off, as quickly as possible, before the random test entries begin.";
            byte status = 1;
            notes = "Intermediate all-off entry";
            for (int channelNmbr = 0; channelNmbr < 24; ++channelNmbr)
            {
                command.Parameters["@recipeID"].Value = recipeID;
                command.Parameters["@channelNmbr"].Value = channelNmbr;
                command.Parameters["@doAt"].Value = startAt;
                command.Parameters["@whenEntered"].Value = whenEntered;
                command.Parameters["@whenEdited"].Value = whenEdited;
                command.Parameters["@enteredBy"].Value = enteredBy;
                command.Parameters["@editedBy"].Value = editedBy;
                command.Parameters["@digitalValue"].Value = digitalValue;
                command.Parameters["@notes"].Value = notes;
                command.Parameters["@status"].Value = status;
                command.ExecuteNonQuery();
            }
            return startAt;
        }

        /// <summary>
        ///     This enables the program to be further tested immediately with current usable recipe data.
        ///     It creates a series of random ON/OFF events for output channels 0 through 23. The effect is a chaotic display of
        ///         LEDs lighting up at random, demonstrating that each output can be written to in a predictable and controlled manner.
        /// </summary>
        static private int EnterRandomControlEvents(int recipeID, int channelNmbr, SqlCommand command, DateTime startAt)
        {
            //byte doDayOfWeek = (byte)DateTime.Now.DayOfWeek;
            DateTime doAt = new();
            doAt = startAt.AddMilliseconds(100);
            DateTime whenEntered;
            DateTime whenEdited;
            Random rnd = new Random();
            TimeSpan hopTime = TimeSpan.FromMilliseconds(rnd.Next(75, 500));
            int enteredBy = 1;
            int editedBy = 1;
            byte digitalValue;
            string notes;
            byte status = 1;
            int errorNmbr = 0;

            whenEntered = whenEdited = DateTime.Now;
            digitalValue = 1;
            notes = "Random test entry";

            command.Parameters["@recipeID"].Value = recipeID;
            command.Parameters["@channelNmbr"].Value = channelNmbr;
            command.Parameters["@doAt"].Value = doAt;
            command.Parameters["@whenEntered"].Value = whenEntered;
            command.Parameters["@whenEdited"].Value = whenEdited;
            command.Parameters["@enteredBy"].Value = enteredBy;
            command.Parameters["@editedBy"].Value = editedBy;
            command.Parameters["@digitalValue"].Value = digitalValue;
            command.Parameters["@notes"].Value = notes;
            command.Parameters["@status"].Value = status;
            command.ExecuteNonQuery();

            /// <remarks>
            ///     This creates a series of one hundred ON-then-OFF events for random channels between 0 and 23.
            ///     The duration of each such event is randomly selected to be between 75 ms and 250 ms.
            /// </remarks> 
            for (int indx = 1; indx < 200; ++indx)
            {
                //  Set channel number to random value between 0 and 23 on even writes
                if (indx % 2 == 0)
                    channelNmbr = rnd.Next(0, 24);
                command.Parameters["@channelNmbr"].Value = channelNmbr;
                //  Flip channel output state
                switch (digitalValue)
                {
                    case 1: digitalValue = 0; break;
                    case 0: digitalValue = 1; break;
                }
                command.Parameters["@digitalValue"].Value = digitalValue;
                //  Increment op-time by random duration between 75 ms and 250 ms
                command.Parameters["@doAt"].Value = doAt.Add(TimeSpan.FromMilliseconds(rnd.Next(75, 250)));
                command.Parameters["@recipeID"].Value = recipeID;
                command.ExecuteNonQuery();
            }
            return errorNmbr;
        }
    }
}