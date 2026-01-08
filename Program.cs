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

//  Written/updated 8 January 2026 by David Fisher
//  Copyright © 2025, 2026 by The ep5 Educational Broadcasting Foundation; all rights reserved


using Microsoft.Data.SqlClient;
using static System.Console;

namespace TableFill
{
    /// <summary>
    ///     This program creates a series of events to be executed by another program that reads from the ControlEvent table.
    ///     It is intended to populate the table with test data for development and debugging purposes. Further, the date and time values
    ///         are set to be relative to the current date and time so that the events begin promptly when the other program is run.
    ///     <b>NOTE: This program is specific to the TaskSchedulerOneTimeSealevel project and its associated database schema. It is for testing
    ///         purposes only and is not intended for production use.</b>
    ///     Its anticipated usage involves calling it from within the program being tested, thus ensuring that the test data is current.
    /// </summary>
    /// <remarks>
    ///     This version of the program is configured for 40 outputs and 8 inputs. The inputs are currently unused.
    ///     With appropriate revisions, including comprehensive logging and error handling, it could be adapted for long-duration hardware testing.
    ///     Equally, it could be modified to run continuously in order to serve as a demo program for the I/O hardware.
    /// </remarks>
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
        const int ON = 1;
        const int OFF = 0;
        static DateTime doAt = new();
        static byte digitalValue;
        static readonly TimeSpan hopTime = TimeSpan.FromMilliseconds(75);       //  Time between sequential events. The value is arbitrary.

        static void Main()
        {
            int recipeID = 1;

            SqlConnection connection = new("Server = System76; Database = TaskSchedulerOneTimeSealevel; Integrated Security = SSPI; TrustServerCertificate = true");
            connection.Open();
            //  Clear out all existing entries in the ControlEvent table.
            //  This is necessitated by the event timing tied to the immediate run of TaskSchedulerOneTimeSealevel.
            string truncateQuery = "truncate table dbo.ControlEvent;";
            SqlCommand truncateCommand = new(truncateQuery, connection);
            truncateCommand.ExecuteNonQuery();
            string query = "INSERT INTO dbo.ControlEvent (recipeID, channelNmbr, doAt, whenEntered, whenEdited, enteredBy, editedBy, digitalValue, notes, status) " +
                "VALUES(@recipeID, @channelNmbr, @doAt, @whenEntered, @whenEdited, @enteredBy, @editedBy, @digitalValue, @notes, @status)";
            SqlCommand command = new(query, connection);

            //  Enter sequential control events first, pulsing up.
            EnterSequentialControlEventsUp(recipeID, 0, command);

            //  Enter sequential control events next, pulsing down.
            EnterSequentialControlEventsDown(recipeID, 39, command);

            //  Enter sequential control events next, turning all outputs ON.
            EnterSequentialControlEventsCumulative(recipeID, 0, command);

            //  Turn all outputs off before entering random control events.
            TurnAllOutputsOff(recipeID, command);

            //  Enter random control events.
            EnterRandomControlEvents(recipeID, 0, command);

            //  Done with engines, Scotty; shut 'er down.
            connection.Close();     //  Granted, a formality, but a familiar one.
            WriteLine("All entries have been written to the database.\n");
            WriteLine("We're done here, Sparky...\nHave a nice day . . . somewhere else.");
            return;
        }

        /// <summary>
        ///     This function starts with digital output channel 0 and creates a series of ON/OFF events for each channel up to channel 39. Each component of the event
        ///         is timed. Thus, each event occurs at an appointed time. The ON and OFF are handled as separate events, each with its own time stamp. While this sequence
        ///         is limited by the physical constaint of the I/O hardware and the Ethernet connection, it is intended to demonstrate that each output can be written 
        ///         to in a predictable and controlled manner.
        /// </summary>
        /// <param name="recipeID">This identifies the database records that will apply to the specific execution of the program.</param>
        /// <param name="channelNmbr">The range of values processed must match the physical layout of the I/O hardware.</param>
        /// <param name="command">The SQL query used to read from the database. It remains constant throughout the program.</param>
        /// <remarks>
        ///     This enables the program to be tested immediately with current, properly timed, single-use recipe data.
        ///     It creates a series of ON/OFF events for channels 0 through 39, with each channel being toggled twice in rapid succession.
        ///     The effect is a parade of LEDs lighting up in numerical succession, demonstrating that each output can be written to when and as required by the user's application.
        /// </remarks>
        static private void EnterSequentialControlEventsUp(int recipeID, int channelNmbr, SqlCommand command)
        {
            doAt = DateTime.Now;
            DateTime whenEntered;
            DateTime whenEdited;
            whenEntered = whenEdited = DateTime.Now;
            int enteredBy = 1;
            int editedBy = 1;
            string notes = "Sequential test entry, pulsing up";
            byte status = 1;
            digitalValue = ON;

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

            for (int indx = 1; indx < 80; ++indx)
            {
                //  Index channel number up after two writes
                if (indx % 2 == 0)
                    command.Parameters["@channelNmbr"].Value = ++channelNmbr;
                //  Flip channel output state
                switch (digitalValue)
                {
                    case ON: digitalValue = OFF; break;
                    case OFF: digitalValue = ON; break;
                }
                command.Parameters["@digitalValue"].Value = digitalValue;
                //  Increment op-time
                doAt = doAt.Add(hopTime);
                command.Parameters["@doAt"].Value = doAt;
                command.Parameters["@recipeID"].Value = recipeID;
                command.ExecuteNonQuery();
            }
            return;
        }

        /// <summary>
        ///     This does the same thing in the same way as the pulsing-up function, but in reverse order.
        /// </summary>
        /// <param name="recipeID">This identifies the database records that will apply to the specific execution of the program.</param>
        /// <param name="channelNmbr">The range of values processed must match the physical layout of the I/O hardware.</param>
        /// <param name="command">The SQL query used to read from the database. It remains constant throughout the program.</param>
        static private void EnterSequentialControlEventsDown(int recipeID, int channelNmbr, SqlCommand command)
        {
            string notes = "Sequential test entry, pulsing down";
            for (int indx = 80; indx > 2; --indx)
            {
                //  Index channel number down after two writes except on first iteration
                if ((indx % 2 == 0) && (channelNmbr > 0))
                    command.Parameters["@channelNmbr"].Value = --channelNmbr;
                //  Flip channel output state
                switch (digitalValue)
                {
                    case ON: digitalValue = OFF; break;
                    case OFF: digitalValue = ON; break;
                }
                command.Parameters["@digitalValue"].Value = digitalValue;
                //  Increment op-time
                doAt = doAt.Add(hopTime);
                command.Parameters["@doAt"].Value = doAt;
                command.Parameters["@recipeID"].Value = recipeID;
                command.Parameters["@notes"].Value = notes;
                command.ExecuteNonQuery();
            }
            return;
        }

        /// <summary>
        ///     This function starts with digital output channel 0 and creates a series of ON events for each channel up to channel 39. Each component of the event
        ///         occurs at a specified time. The effect is all outputs being turned ON in rapid succession.
        /// </summary>
        /// <param name="recipeID">This identifies the database records that will apply to the specific execution of the program.</param>
        /// <param name="channelNmbr">This controls which digital output channel starts the sequence.</param>
        /// <param name="command">The SQL query used to read from the database. It remains constant throughout the program.</param>
        static private void EnterSequentialControlEventsCumulative(int recipeID, int channelNmbr, SqlCommand command)
        {
            string notes = "Sequential test entry, cumulative ON";
            digitalValue = ON;
            for (int indx = 0; indx < 40; ++indx)
            {
                command.Parameters["@channelNmbr"].Value = channelNmbr++;   // N.B.: Post-increment ONLY.
                command.Parameters["@digitalValue"].Value = digitalValue;
                //  Increment op-time
                doAt = doAt.Add(hopTime);
                command.Parameters["@doAt"].Value = doAt;
                command.Parameters["@recipeID"].Value = recipeID;
                command.Parameters["@notes"].Value = notes;
                command.ExecuteNonQuery();
            }
            return;
        }

        /// <summary>
        ///     This turns all digital outputs off, as quickly as possible, before the random test entries begin.
        ///     The function turns all digital outputs OFF and does so in a manner limited by the physical constraints of the I/O hardware and the Ethernet connection.
        ///     That is, all outputs have one single event time, obliging the hardware to execute them as quickly as the physical limitations allow.
        ///     The effect is all LEDs being turned OFF as close to simultaneously as possible. The result is a graphical demonstration of I/O cycle speed.
        /// </summary>
        /// <remarks>
        ///     This function is implemented with an individual OFF command for each channel, all such events sharing the same when-to-execute timestamp.
        ///     A possible alternative, depending upon the capabilities of the I/O hardware, would be to issue a single command that turns all outputs OFF simultaneously.
        /// </remarks>
        static private void TurnAllOutputsOff(int recipeID, SqlCommand command)
        {
            DateTime whenEntered = DateTime.Now;
            DateTime whenEdited = DateTime.Now;
            doAt = doAt.AddMilliseconds(100);
            int enteredBy = 1;
            int editedBy = 1;
            byte digitalValue = OFF;
            string notes = "Turn all digital outputs off, as quickly as possible, before the random test entries begin.";
            byte status = 1;
            notes = "Intermediate all-off entry";
            for (int channelNmbr = 0; channelNmbr < 40; ++channelNmbr)
            {
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
            }
            return;
        }

        /// <summary>
        ///     This enables the program to be further tested immediately with current usable recipe data.
        ///     It creates a series of random ON/OFF events for output channels 0 through 23. The effect is a chaotic display of
        ///         LEDs lighting up at random, demonstrating that each output can be written to in a predictable and controlled manner.
        /// </summary>
        static private void EnterRandomControlEvents(int recipeID, int channelNmbr, SqlCommand command)
        {
            doAt = doAt.AddMilliseconds(100);
            DateTime whenEntered;
            DateTime whenEdited;
            Random rnd = new Random();
            TimeSpan hopTime = TimeSpan.FromMilliseconds(rnd.Next(75, 500));
            int enteredBy = 1;
            int editedBy = 1;
            byte digitalValue;
            string notes = "Random test entry";
            byte status = 1;

            whenEntered = whenEdited = DateTime.Now;
            digitalValue = ON;

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
            ///     This creates a series of one hundred ON-then-OFF events for random channels between 0 and 39.
            ///     The duration of each such event is randomly selected to be between 75 ms and 250 ms.
            /// </remarks> 
            for (int indx = 1; indx < 200; ++indx)
            {
                //  Set channel number to random value between 0 and 23 on even writes
                if (indx % 2 == 0)
                    channelNmbr = rnd.Next(0, 40);
                command.Parameters["@channelNmbr"].Value = channelNmbr;
                //  Flip channel output state.
                switch (digitalValue)
                {
                    case ON: digitalValue = OFF; break;
                    case OFF: digitalValue = ON; break;
                }
                command.Parameters["@digitalValue"].Value = digitalValue;
                //  Increment op-time by random duration between 75 ms and 500 ms.
                command.Parameters["@doAt"].Value = doAt.Add(TimeSpan.FromMilliseconds(rnd.Next(75, 500)));
                command.Parameters["@recipeID"].Value = recipeID;
                command.ExecuteNonQuery();
            }
            return;
        }
    }
}