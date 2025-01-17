using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using daemon_console;
using daemon_console.GraphCrud;
using Microsoft.Graph;
using MySql.Data.MySqlClient;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;



namespace UUIDproducer
{
    class Consumer
    {

        public static void getMessage()
        {

            string cs = "";
            
            try
            {
                cs = System.IO.File.ReadAllText("cs.txt");
            


            var factory = new ConnectionFactory() { HostName = "10.3.56.6" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: "direct_logs",
                type: "direct");

                var queueName = "officeQueue";
                channel.QueueDeclare(queue: queueName,
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);


                channel.QueueBind(queue: queueName,
                exchange: "direct_logs",
                routingKey: "event");
                channel.QueueBind(queue: queueName,
                exchange: "direct_logs",
                routingKey: "user");
                channel.QueueBind(queue: queueName,
                exchange: "direct_logs",
                routingKey: "Office");
                Console.WriteLine(" [*] Waiting for messages.");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var routingKey = ea.RoutingKey;
                    Console.WriteLine(" [x] Received '{0}':'{1}'", routingKey, message);

                    bool xmlValidation = true;
                    bool xmlValidationUser = true;
                    bool xmlValidationSubscribe = true;
                    bool xmlValidationError = true;

                    //xsd event validation
                    XmlSchemaSet schema = new XmlSchemaSet();
                    schema.Add("", "EventSchema.xsd");

                    XmlSchemaSet schemaSubscribe = new XmlSchemaSet();
                    schemaSubscribe.Add("", "SubscribeSchema.xsd");

                    XmlSchemaSet schemaUser = new XmlSchemaSet();
                    schemaUser.Add("", "UserSchema.xsd");

                    XmlSchemaSet schemaError = new XmlSchemaSet();
                    schemaError.Add("", "Errorxsd.xsd");

                    //XDocument xml = XDocument.Parse(message, LoadOptions.SetLineInfo);
                    XmlDocument xmlDoc = new XmlDocument();
                    XDocument xml = new XDocument();
                    try
                    {
                        xmlDoc.LoadXml(message);
                        xml = XDocument.Parse(xmlDoc.OuterXml);
                        xml.Validate(schema, (sender, e) =>
                        {
                            xmlValidation = false;
                        });
                        xml.Validate(schemaUser, (sender, e) =>
                        {
                            xmlValidationUser = false;
                        });
                        xml.Validate(schemaSubscribe, (sender, e) =>
                        {
                            xmlValidationSubscribe = false;
                        });
                        xml.Validate(schemaError, (sender, e) =>
                        {
                            xmlValidationError = false;
                        });




                        string dateTimeParsedXML = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss%K").ToString();

                        //Alter XML to change
                        XmlDocument docAlter = new XmlDocument();
                        XmlDocument docAlterSub = new XmlDocument();
                        XmlDocument docAlterError = new XmlDocument();
                        XmlDocument docAlterLog = new XmlDocument();

                        if (xmlValidation)
                        {
                        
                            Console.WriteLine("XML is valid");

                            //XML head
                            XmlNode myEventUUID = xmlDoc.SelectSingleNode("//UUID");
                            XmlNode myMethodNode = xmlDoc.SelectSingleNode("//method");
                            XmlNode myOriginNode = xmlDoc.SelectSingleNode("//origin");
                            XmlNode myUserId = xmlDoc.SelectSingleNode("//organiserUUID");
                            XmlNode myOrganiserSourceId = xmlDoc.SelectSingleNode("//organiserSourceEntityId");
                            XmlNode mySourceEntityId = xmlDoc.SelectSingleNode("//sourceEntityId");
                            //XML body
                            XmlNode myEventName = xmlDoc.SelectSingleNode("//name");
                            XmlNode myStartEvent = xmlDoc.SelectSingleNode("//startEvent");
                            XmlNode myEndEvent = xmlDoc.SelectSingleNode("//endEvent");
                            XmlNode myDescription = xmlDoc.SelectSingleNode("//description");
                            XmlNode myLocation = xmlDoc.SelectSingleNode("//location");

                            //Create Event comes from FrontEnd and we pass it to UUID to compare
                            if (myOriginNode.InnerXml == "FrontEnd" && myMethodNode.InnerXml == "CREATE" && myOrganiserSourceId.InnerXml == "" && routingKey == "event")
                            {
                                Console.WriteLine("Got a message from " + myOriginNode.InnerXml);
                                Console.WriteLine("Updating origin \"" + myOriginNode.InnerXml + "\" of XML...");

                           

                                //XmlWriterSettings settings = new XmlWriterSettings();
                                //settings.Indent = true;
                                //XmlWriter writer = XmlWriter.Create("Alter.xml", settings);
                                //doc1.Save(writer);


                                docAlter.Load("Alter.xml");
                                docAlter = xmlDoc;

                                docAlter.SelectSingleNode("//event/header/origin").InnerText = "Office";
                                docAlter.Save("Alter.xml");


                                docAlter.Save(Console.Out);
                                //Console.WriteLine(docAlter.InnerXml);
                                //Console.WriteLine(docMessage.InnerXml);
                                //Console.WriteLine(docMessageConverted.InnerXml);


                                Task task = new Task(() => Producer.sendMessage(docAlter.InnerXml, "UUID"));
                                task.Start();

                                Console.WriteLine("Origin changed to Office, sending now it to UUID...");

                            }
                        
                            //Create Event comes from UUID we use it and pass it again to UUID (Last step create)
                            else if (myOriginNode.InnerXml == "UUID" && myMethodNode.InnerXml == "CREATE" && myOrganiserSourceId.InnerXml != "" && routingKey == "Office")
                            {
                                using var con = new MySqlConnection(cs);
                                con.Open();


                                    string email = "";
                                string name = "";

                                var sqlForEmail = "SELECT email FROM User WHERE userId = '" + myOrganiserSourceId.InnerXml + "'";
                                using var cmd1 = new MySqlCommand(sqlForEmail, con);
                                MySqlDataReader dr = cmd1.ExecuteReader();
                            
                                if (dr.Read())
                                  {
                                    email = dr[0].ToString();
                                    email = dr[0].ToString().Remove(dr[0].ToString().Length - 24);
                                    email = $"{email}@flowupdesiderius.onmicrosoft.com";
                                    Console.WriteLine(email);
                                  }
                                dr.Close();

                                var sqlForFirstname = "SELECT firstname FROM User WHERE userId = '" + myOrganiserSourceId.InnerXml + "'";
                                using var cmd2 = new MySqlCommand(sqlForFirstname, con);
                                MySqlDataReader dr1 = cmd2.ExecuteReader();

                                if (dr1.Read())
                                  {
                                      name = dr1[0].ToString();
                                      Console.WriteLine(name);
                                  }
                                  dr1.Close();

                                var sqlForLastname = "SELECT lastname FROM User WHERE userId = '" + myOrganiserSourceId.InnerXml + "'";
                                using var cmd3 = new MySqlCommand(sqlForLastname, con);
                                MySqlDataReader dr2 = cmd3.ExecuteReader();

                                 if (dr2.Read())
                                  {
                                      name += String.Join(" ", dr2[0].ToString());
                                      Console.WriteLine(name);
                                  }
                                 dr2.Close();

                                   Console.WriteLine(myStartEvent.InnerXml);
                                List<Attendee> attendeesAtCreate = new List<Attendee>();
                                string eventId= "fout";
                                try
                                {
                                    string startTimeEvent = myStartEvent.InnerXml.Substring(0, (myStartEvent.InnerXml.Length - 6));
                                    string endTimeEvent = myEndEvent.InnerXml.Substring(0, (myEndEvent.InnerXml.Length - 6));
                                    eventId= Program.RunAsync("create",myEventName.InnerXml,myDescription.InnerXml, startTimeEvent, endTimeEvent,
                                        myLocation.InnerXml, attendeesAtCreate,email,name, true,"null").GetAwaiter().GetResult();
                                
                                }
                                catch (Exception ex)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine(ex.Message);
                                    Console.ResetColor();
                                    docAlterLog.Load("AlterLog.xml");


                                        docAlterLog.Load("AlterLog.xml");

                                        docAlterLog.SelectSingleNode("//log/header/code").InnerText = "3000";
                                        docAlterLog.SelectSingleNode("//log/header/timestamp").InnerText = dateTimeParsedXML;
                                        docAlterLog.SelectSingleNode("//log/body/objectUUID").InnerText = myEventUUID.InnerXml;
                                        docAlterLog.SelectSingleNode("//log/body/objectSourceId").InnerText = "Create Event";
                                        docAlterLog.SelectSingleNode("//log/body/description").InnerText = "Probleem met RunAsync around line 248";
                                        docAlterLog.Save("AlterLog.xml");
                                        docAlterLog.Save(Console.Out);

                                        Task taskLogAsync = new Task(() => Producer.sendMessageLogging(docAlterLog.InnerXml, "logging"));
                                        taskLogAsync.Start();
                                    }

                                Console.WriteLine("Got a message from " + myOriginNode.InnerXml);
                                Console.WriteLine("Putting data in database and calendar");

                            

                                Console.WriteLine(eventId);
                                var sql = "INSERT INTO Event(name, userId, graphResponse, startEvent, endEvent, description, location) VALUES(@name, @userId, @graphResponse, @startEvent, @endEvent, @description, @location); SELECT @@IDENTITY";
                                //var sql = "INSERT INTO Event(name, userId, startEvent, endEvent, description, location) VALUES(@name, @userId, @startEvent, @endEvent, @description, @location); SELECT @@IDENTITY";

                                using var cmd = new MySqlCommand(sql, con);

                                //Parse data to put into database
                                DateTime parsedDateStart;

                                cmd.Parameters.AddWithValue("@name", myEventName.InnerXml);
                                cmd.Parameters.AddWithValue("@userId", myOrganiserSourceId.InnerXml);
                                cmd.Parameters.AddWithValue("@graphResponse", eventId);
                                cmd.Parameters.AddWithValue("@startEvent", myStartEvent.InnerXml);
                                cmd.Parameters.AddWithValue("@endEvent", myEndEvent.InnerXml);
                                cmd.Parameters.AddWithValue("@description", myDescription.InnerXml);
                                cmd.Parameters.AddWithValue("@location", myLocation.InnerXml);

                                int iNewRowIdentity = Convert.ToInt32(cmd.ExecuteScalar());
                                Console.WriteLine("Event Id in database is: " + iNewRowIdentity);


                                Console.WriteLine("Event inserted in database");

                                docAlter.Load("Alter.xml");
                                docAlter = xmlDoc;

                                docAlter.SelectSingleNode("//event/header/origin").InnerText = "Office";
                                docAlter.SelectSingleNode("//event/header/sourceEntityId").InnerText = iNewRowIdentity.ToString();
                                docAlter.Save("Alter.xml");

                                docAlter.Save(Console.Out);

                                Task task = new Task(() => Producer.sendMessage(docAlter.InnerXml, "UUID"));
                                task.Start();


                                docAlterLog.Load("AlterLog.xml");

                                docAlterLog.SelectSingleNode("//log/header/code").InnerText = "3000";
                                docAlterLog.SelectSingleNode("//log/header/timestamp").InnerText = dateTimeParsedXML;
                                docAlterLog.SelectSingleNode("//log/body/objectUUID").InnerText = myEventUUID.InnerXml;
                                docAlterLog.SelectSingleNode("//log/body/objectSourceId").InnerText = "Create Event";
                                docAlterLog.SelectSingleNode("//log/body/description").InnerText = "Event created by Office";
                                docAlterLog.Save("AlterLog.xml");
                                docAlterLog.Save(Console.Out);

                                Task taskLog = new Task(() => Producer.sendMessageLogging(docAlterLog.InnerXml, "logging"));
                                taskLog.Start();


                                Console.WriteLine("Sending message to UUID and Loggs(Last step)...");
                            }

                            //Update Event comes from FrontEnd and we pass it to UUID to compare
                            if (myOriginNode.InnerXml == "FrontEnd" && myMethodNode.InnerXml == "UPDATE" && myOrganiserSourceId.InnerXml == "" && routingKey == "event")
                            {
                                Console.WriteLine("Got a message from " + myOriginNode.InnerXml);
                                Console.WriteLine("Updating origin from \"" + myOriginNode.InnerXml + " to \"Office\"");


                                docAlter.Load("Alter.xml");
                                docAlter = xmlDoc;

                                docAlter.SelectSingleNode("//event/header/origin").InnerText = "Office";
                                docAlter.Save("Alter.xml");
                                docAlter.Save(Console.Out);


                                Task task = new Task(() => Producer.sendMessage(docAlter.InnerXml, "UUID"));
                                task.Start();

                                Console.WriteLine("Origin changed to Office, sending now it to UUID...");

                            }

                            //Update Event comes from UUID, we update our side and tell the UUID
                            else if (myOriginNode.InnerXml == "UUID" && myMethodNode.InnerXml == "UPDATE" && mySourceEntityId.InnerXml != "" && routingKey == "Office")
                            {
                                Console.WriteLine("Got a message from " + myOriginNode.InnerXml);
                                //Console.WriteLine("Source id is: " + myDescription.InnerXml);
                                Console.WriteLine("Updating event data in database and calendar");
                            
                                using var con = new MySqlConnection(cs);
                                con.Open();

                                    var sqlForId = "SELECT graphResponse FROM Event WHERE eventId = '" + mySourceEntityId.InnerXml + "'";
                                    using var cmd1 = new MySqlCommand(sqlForId, con);
                                    MySqlDataReader dr = cmd1.ExecuteReader();
                                    string eventId = "";
                                    if (dr.Read())
                                    {
                                        eventId = dr[0].ToString();
                                        Console.WriteLine(eventId);
                                    }
                                    dr.Close();


                                    try
                                    {
                                        List<Attendee> attendeesAtCreate = new List<Attendee>();
                                        string startTimeEvent = myStartEvent.InnerXml.Substring(0, (myStartEvent.InnerXml.Length - 6));
                                        string endTimeEvent = myEndEvent.InnerXml.Substring(0, (myEndEvent.InnerXml.Length - 6));
                                        Program.RunAsync("update", myEventName.InnerXml, myDescription.InnerXml, startTimeEvent, endTimeEvent,
                                        myLocation.InnerXml, attendeesAtCreate,"","", true, eventId).GetAwaiter().GetResult();

                                    }
                                    catch (Exception ex)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine(ex.Message);
                                        Console.ResetColor();
                                        docAlterLog.Load("AlterLog.xml");

                                        docAlterLog.SelectSingleNode("//log/header/code").InnerText = "4000";
                                        docAlterLog.SelectSingleNode("//log/header/timestamp").InnerText = dateTimeParsedXML;
                                        docAlterLog.SelectSingleNode("//log/body/objectUUID").InnerText = myEventUUID.InnerXml;
                                        docAlterLog.SelectSingleNode("//log/body/objectSourceId").InnerText = "Update Event";
                                        docAlterLog.SelectSingleNode("//log/body/description").InnerText = "Probleem met RunAsync around line 396";
                                        docAlterLog.Save("AlterLog.xml");
                                        docAlterLog.Save(Console.Out);

                                        Task taskLogAsync1 = new Task(() => Producer.sendMessageLogging(docAlterLog.InnerXml, "logging"));
                                        taskLogAsync1.Start();
                                    }

                                    var sql = "UPDATE Event SET name = @name, startEvent = @startEvent, endEvent = @endEvent, description = @description, location = @location WHERE eventId = '" + mySourceEntityId.InnerXml + "'";
                                    using var cmd = new MySqlCommand(sql, con);



                                cmd.Parameters.AddWithValue("@name", myEventName.InnerXml);
                                cmd.Parameters.AddWithValue("@startEvent", myStartEvent.InnerXml);
                                cmd.Parameters.AddWithValue("@endEvent", myEndEvent.InnerXml);
                                cmd.Parameters.AddWithValue("@description", myDescription.InnerXml);
                                cmd.Parameters.AddWithValue("@location", myLocation.InnerXml);
                                cmd.Prepare();
                                cmd.ExecuteNonQuery();
                                Console.WriteLine("Event inserted in database");

                                docAlter.Load("Alter.xml");
                                docAlter = xmlDoc;

                                docAlter.SelectSingleNode("//event/header/origin").InnerText = "Office";
                                docAlter.SelectSingleNode("//event/header/sourceEntityId").InnerText = mySourceEntityId.InnerXml;
                                docAlter.Save("Alter.xml");

                                docAlter.Save(Console.Out);

                                Task task = new Task(() => Producer.sendMessage(docAlter.InnerXml, "UUID"));
                                task.Start();

                                    docAlterLog.Load("AlterLog.xml");

                                    docAlterLog.SelectSingleNode("//log/header/code").InnerText = "4000";
                                    docAlterLog.SelectSingleNode("//log/header/timestamp").InnerText = dateTimeParsedXML;
                                    docAlterLog.SelectSingleNode("//log/body/objectUUID").InnerText = myEventUUID.InnerXml;
                                    docAlterLog.SelectSingleNode("//log/body/objectSourceId").InnerText = "Update Event";
                                    docAlterLog.SelectSingleNode("//log/body/description").InnerText = "Update Event succesfully done";
                                    docAlterLog.Save("AlterLog.xml");
                                    docAlterLog.Save(Console.Out);

                                    Task taskLogAsync = new Task(() => Producer.sendMessageLogging(docAlterLog.InnerXml, "logging"));
                                    taskLogAsync.Start();

                                    Console.WriteLine("Sending update message to UUID and logs(last step)...");
                            }
                        
                            //Delete Event comes from Front end and we pass it to UUID to compare
                            if (myOriginNode.InnerXml == "FrontEnd" && myMethodNode.InnerXml == "DELETE" && myOrganiserSourceId.InnerXml == "" && routingKey == "event")
                            {



                                Console.WriteLine("Got a message from " + myOriginNode.InnerXml);
                                Console.WriteLine("Updating origin from \"" + myOriginNode.InnerXml + " to \"Office\"");


                                docAlter.Load("Alter.xml");
                                docAlter = xmlDoc;

                                docAlter.SelectSingleNode("//event/header/origin").InnerText = "Office";
                                docAlter.Save("Alter.xml");
                                docAlter.Save(Console.Out);

                                Task task = new Task(() => Producer.sendMessage(docAlter.InnerXml, "UUID"));
                                task.Start();

                                Console.WriteLine("Origin changed to Office, sending delete now it to UUID...");

                            }
                        
                            //Get delete info from UUID and delete it in db and in calendar
                            else if (myOriginNode.InnerXml == "UUID" && myMethodNode.InnerXml == "DELETE" && myOrganiserSourceId.InnerXml != "" && routingKey == "Office")
                            {
                                Console.WriteLine("Got a delete message from " + myOriginNode.InnerXml);
                                Console.WriteLine("The full message from the UUID is: " + xmlDoc.InnerXml);
                                //Console.WriteLine("Source id is: " + myDescription.InnerXml);
                                Console.WriteLine("Deleting event data in database and calendar");

                                using var con = new MySqlConnection(cs);
                                con.Open();

                                var sqlForId = "SELECT graphResponse FROM Event WHERE eventId = '" + mySourceEntityId.InnerXml + "'";
                                using var cmd = new MySqlCommand(sqlForId, con);
                                MySqlDataReader dr = cmd.ExecuteReader();
                                string eventId="";
                                if (dr.Read())
                                {
                                    eventId = dr[0].ToString();
                                    Console.WriteLine(eventId);
                                }
                                dr.Close();
                            

                                try
                                {
                                    List<Attendee> attendeesAtCreate = new List<Attendee>();
                                    string startTimeEvent = myStartEvent.InnerXml.Substring(0, (myStartEvent.InnerXml.Length - 6));
                                    string endTimeEvent = myEndEvent.InnerXml.Substring(0, (myEndEvent.InnerXml.Length - 6));
                                    Program.RunAsync("delete", "", "", "", "","", attendeesAtCreate,"","", true, eventId).GetAwaiter().GetResult();

                                }
                                catch (Exception ex)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine(ex.Message);
                                    Console.ResetColor();
                                        docAlterLog.Load("AlterLog.xml");

                                        docAlterLog.SelectSingleNode("//log/header/code").InnerText = "5000";
                                        docAlterLog.SelectSingleNode("//log/header/timestamp").InnerText = dateTimeParsedXML;
                                        docAlterLog.SelectSingleNode("//log/body/objectUUID").InnerText = myEventUUID.InnerXml;
                                        docAlterLog.SelectSingleNode("//log/body/objectSourceId").InnerText = "Update Event";
                                        docAlterLog.SelectSingleNode("//log/body/description").InnerText = "Probleem met RunAsync around line 517";
                                        docAlterLog.Save("AlterLog.xml");
                                        docAlterLog.Save(Console.Out);

                                        Task taskLogAsync1 = new Task(() => Producer.sendMessageLogging(docAlterLog.InnerXml, "logging"));
                                        taskLogAsync1.Start();
                                    }


                                    try
                                    {
                                        var sql = "DELETE FROM Event WHERE eventId = '" + mySourceEntityId.InnerXml + "'";
                                        using var cmd1 = new MySqlCommand(sql, con);
                                        cmd1.ExecuteNonQuery();
                                    }
                                    catch (Exception ex)
                                    {
                                        docAlterLog.Load("AlterLog.xml");

                                        docAlterLog.SelectSingleNode("//log/header/code").InnerText = "5000";
                                        docAlterLog.SelectSingleNode("//log/header/timestamp").InnerText = dateTimeParsedXML;
                                        docAlterLog.SelectSingleNode("//log/body/objectUUID").InnerText = myEventUUID.InnerXml;
                                        docAlterLog.SelectSingleNode("//log/body/objectSourceId").InnerText = "Delete Event";
                                        docAlterLog.SelectSingleNode("//log/body/description").InnerText = "Probleem met Delete in Databank around line 540";
                                        docAlterLog.Save("AlterLog.xml");
                                        docAlterLog.Save(Console.Out);

                                        Task taskLogAsync1 = new Task(() => Producer.sendMessageLogging(docAlterLog.InnerXml, "logging"));
                                        taskLogAsync1.Start();
                                    }

                                        docAlterLog.Load("AlterLog.xml");

                                    docAlterLog.SelectSingleNode("//log/header/code").InnerText = "5000";
                                    docAlterLog.SelectSingleNode("//log/header/timestamp").InnerText = dateTimeParsedXML;
                                    docAlterLog.SelectSingleNode("//log/body/objectUUID").InnerText = myEventUUID.InnerXml;
                                    docAlterLog.SelectSingleNode("//log/body/objectSourceId").InnerText = "Delete Event";
                                    docAlterLog.SelectSingleNode("//log/body/description").InnerText = "Event deleted succesfully";
                                    docAlterLog.Save("AlterLog.xml");
                                    docAlterLog.Save(Console.Out);

                                    Task taskLogAsync = new Task(() => Producer.sendMessageLogging(docAlterLog.InnerXml, "logging"));
                                    taskLogAsync.Start();

                                    Console.WriteLine("Event deleted in database sending to Logs(Last step)");

                            }
                        }
                        else if (xmlValidationUser)
                        {
                            XmlDocument docAlterUser = new XmlDocument();
                            Console.WriteLine("XML user is valid");

                            //XML HEAD
                            XmlNode myMethodNodeUser = xmlDoc.SelectSingleNode("//method");
                            XmlNode myOriginNodeUser = xmlDoc.SelectSingleNode("//origin");
                            XmlNode mySourceIdUser = xmlDoc.SelectSingleNode("//sourceEntityId");
                            XmlNode myUserVersion = xmlDoc.SelectSingleNode("//version");

                            //XML BODY
                            XmlNode myFirstName = xmlDoc.SelectSingleNode("//firstname");
                            XmlNode myLastName = xmlDoc.SelectSingleNode("//lastname");
                            XmlNode myEmail = xmlDoc.SelectSingleNode("//email");
                            XmlNode myBirthday = xmlDoc.SelectSingleNode("//birthday");
                            XmlNode myRole = xmlDoc.SelectSingleNode("//role");
                            XmlNode myStudy = xmlDoc.SelectSingleNode("//study");


                            //CREATE User, message from AD
                            //if (myOriginNodeUser.InnerXml == "AD" && myMethodNodeUser.InnerXml == "CREATE" && mySourceIdUser.InnerXml == "" && routingKey == "user")
                            if (myOriginNodeUser.InnerXml == "AD" && myMethodNodeUser.InnerXml == "CREATE" && routingKey == "user")
                            {
                                Console.WriteLine("Got a message from " + myOriginNodeUser.InnerXml);
                                Console.WriteLine("Updating origin \"" + myOriginNodeUser.InnerXml + "\" of XML...");



                                docAlterUser.Load("AlterUser.xml");
                                docAlterUser = xmlDoc;

                                docAlterUser.SelectSingleNode("//user/header/origin").InnerText = "Office";
                                docAlterUser.SelectSingleNode("//user/header/sourceEntityId").InnerText = "";
                                docAlterUser.Save("AlterUser.xml");


                                docAlterUser.Save(Console.Out);


                                Task task = new Task(() => Producer.sendMessage(docAlterUser.InnerXml, "UUID"));
                                task.Start();

                                Console.WriteLine("Origin changed to Office, sending now it to UUID...");
                            }
                            //Message from UUID CREATE user now
                            //else if (myOriginNodeUser.InnerXml == "UUID" && myMethodNodeUser.InnerXml == "CREATE" && mySourceIdUser.InnerXml == "" && routingKey == "Office")
                            else if (myOriginNodeUser.InnerXml == "UUID" && myMethodNodeUser.InnerXml == "CREATE" && routingKey == "Office")
                            {

                                Console.WriteLine("Got a message from " + myOriginNodeUser.InnerXml);
                                Console.WriteLine("Creating user, and putting it in database");


                                //connection to the database
                                using var con = new MySqlConnection(cs);
                                con.Open();

                                var sql = "INSERT INTO User(firstname,lastname,email,birthday,role,study) VALUES(@firstname, @lastname, @email, @birthday,@role,@study); SELECT @@IDENTITY";
                                using var cmd = new MySqlCommand(sql, con);

                                //Parse data to put into database
                                DateTime parsedBirthday;

                                parsedBirthday = DateTime.Parse(myBirthday.InnerXml, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

                                cmd.Parameters.AddWithValue("@firstname", myFirstName.InnerXml);
                                cmd.Parameters.AddWithValue("@lastname", myLastName.InnerXml);
                                cmd.Parameters.AddWithValue("@email", myEmail.InnerXml);
                                cmd.Parameters.AddWithValue("@birthday", parsedBirthday);
                                cmd.Parameters.AddWithValue("@role", myRole.InnerXml);
                                cmd.Parameters.AddWithValue("@study", myStudy.InnerXml);


                                int iNewRowIdentity = Convert.ToInt32(cmd.ExecuteScalar());
                                Console.WriteLine("User Id in database is: " + iNewRowIdentity);
                                Console.WriteLine("User inserted in database");


                                docAlterUser.Load("AlterUser.xml");
                                docAlterUser = xmlDoc;

                                docAlterUser.SelectSingleNode("//user/header/origin").InnerText = "Office";
                                docAlterUser.SelectSingleNode("//user/header/sourceEntityId").InnerText = iNewRowIdentity.ToString();
                                docAlterUser.Save("AlterUser.xml");


                                docAlter.Save(Console.Out);


                                Task task = new Task(() => Producer.sendMessage(docAlterUser.InnerXml, "UUID"));
                                task.Start();

                                    docAlterLog.Load("AlterLog.xml");

                                    docAlterLog.SelectSingleNode("//log/header/code").InnerText = "3000";
                                    docAlterLog.SelectSingleNode("//log/header/timestamp").InnerText = dateTimeParsedXML;
                                    docAlterLog.SelectSingleNode("//log/body/objectSourceId").InnerText = "Create User";
                                    docAlterLog.SelectSingleNode("//log/body/description").InnerText = "User created succesfully";
                                    docAlterLog.Save("AlterLog.xml");
                                    docAlterLog.Save(Console.Out);

                                    Task taskLogAsync = new Task(() => Producer.sendMessageLogging(docAlterLog.InnerXml, "logging"));
                                    taskLogAsync.Start();

                                    Console.WriteLine("\nSending creating user message to UUID and logs...(Last step)");
                            }
                            //UPDATE user comes from AD and we pass it to UUID to compare
                            //if (myOriginNodeUser.InnerXml == "AD" && myMethodNodeUser.InnerXml == "UPDATE" && mySourceIdUser.InnerXml == "" && routingKey == "user")
                            if (myOriginNodeUser.InnerXml == "AD" && myMethodNodeUser.InnerXml == "UPDATE" && routingKey == "user")
                            {

                                Console.WriteLine("Got a message from " + myOriginNodeUser.InnerXml);
                                Console.WriteLine("updating user, and putting it in database");

                                docAlterUser.Load("AlterUser.xml");
                                docAlterUser = xmlDoc;

                                docAlterUser.SelectSingleNode("//user/header/origin").InnerText = "Office";
                                docAlterUser.SelectSingleNode("//user/header/sourceEntityId").InnerText = "";
                                docAlterUser.Save("AlterUser.xml");
                                docAlterUser.Save(Console.Out);



                                Task task = new Task(() => Producer.sendMessage(docAlterUser.InnerXml, "UUID"));
                                task.Start();

                                Console.WriteLine("Origin changed to Office, sending now it to UUID...");
                            }
                            //update user comes from UUID we update our side and tell the UUID
                            //else if (myOriginNodeUser.InnerXml == "UUID" && myMethodNodeUser.InnerXml == "UPDATE" && mySourceIdUser.InnerXml == "" && routingKey == "Office")
                            else if (myOriginNodeUser.InnerXml == "UUID" && myMethodNodeUser.InnerXml == "UPDATE" && routingKey == "Office")
                            {
                                Console.WriteLine("Got a message from " + myOriginNodeUser.InnerXml);
                                //Console.WriteLine("Source id is: " + myDescription.InnerXml);
                                Console.WriteLine("Updating user data in database and calendar");


                                try
                                {


                                    using var con = new MySqlConnection(cs);
                                    con.Open();


                                    var sql = "UPDATE User SET firstname = @firstname, lastname  = @lastname, email = @email, birthday = @birthday, role = @role, study = @study WHERE userId = '" + mySourceIdUser.InnerXml + "'";
                                    using var cmd = new MySqlCommand(sql, con);

                                    //Parse data to put into database
                                    DateTime parsedBirthday;

                                    parsedBirthday = DateTime.Parse(myBirthday.InnerXml, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

                                    //cmd.Parameters.AddWithValue("@userId", mySourceIdUser.InnerXml);
                                    cmd.Parameters.AddWithValue("@firstname", myFirstName.InnerXml);
                                    cmd.Parameters.AddWithValue("@lastname", myLastName.InnerXml);
                                    cmd.Parameters.AddWithValue("@email", myEmail.InnerXml);
                                    cmd.Parameters.AddWithValue("@birthday", parsedBirthday);
                                    cmd.Parameters.AddWithValue("@role", myRole.InnerXml);
                                    cmd.Parameters.AddWithValue("@study", myStudy.InnerXml);
                                    cmd.Prepare();
                                    cmd.ExecuteNonQuery();

                                    Console.WriteLine("User inserted in database");
                                }
                                catch (SqlException e)
                                {
                                    Console.WriteLine("User not yet in our database");
                                    Console.WriteLine("Exception is: " + e.Message);
                                        docAlterLog.Load("AlterLog.xml");

                                        docAlterLog.SelectSingleNode("//log/header/code").InnerText = "4000";
                                        docAlterLog.SelectSingleNode("//log/header/timestamp").InnerText = dateTimeParsedXML;
                                        docAlterLog.SelectSingleNode("//log/body/objectSourceId").InnerText = "Update User";
                                        docAlterLog.SelectSingleNode("//log/body/description").InnerText = "User updated db error 741";
                                        docAlterLog.Save("AlterLog.xml");
                                        docAlterLog.Save(Console.Out);

                                        Task taskLogAsync2 = new Task(() => Producer.sendMessageLogging(docAlterLog.InnerXml, "logging"));
                                        taskLogAsync2.Start();
                                    }

                                //int iNewRowIdentity = Convert.ToInt32(cmd.ExecuteScalar());
                                //Console.WriteLine("User Id in database is: " + iNewRowIdentity);



                                docAlterUser.Load("AlterUser.xml");
                                docAlterUser = xmlDoc;

                                docAlterUser.SelectSingleNode("//user/header/origin").InnerText = "Office";
                                //docAlter.SelectSingleNode("//user/header/userId").InnerText = mySourceIdUser.InnerXml;
                                docAlterUser.Save("AlterUser.xml");


                                docAlterUser.Save(Console.Out);


                                Task task = new Task(() => Producer.sendMessage(docAlterUser.InnerXml, "UUID"));
                                task.Start();

                                    docAlterLog.Load("AlterLog.xml");

                                    docAlterLog.SelectSingleNode("//log/header/code").InnerText = "4000";
                                    docAlterLog.SelectSingleNode("//log/header/timestamp").InnerText = dateTimeParsedXML;
                                    docAlterLog.SelectSingleNode("//log/body/objectSourceId").InnerText = "Update User";
                                    docAlterLog.SelectSingleNode("//log/body/description").InnerText = "User updated succesfully";
                                    docAlterLog.Save("AlterLog.xml");
                                    docAlterLog.Save(Console.Out);

                                    Task taskLogAsync = new Task(() => Producer.sendMessageLogging(docAlterLog.InnerXml, "logging"));
                                    taskLogAsync.Start();

                                    Console.WriteLine("Sending update user message to UUID and logs(Last step)...");


                            }
                            //Delete user comes from AD we update our side and tell the UUID
                            //if (myOriginNodeUser.InnerXml == "UUID" && myMethodNodeUser.InnerXml == "DELETE" && mySourceIdUser.InnerXml == "" && routingKey == "Office")
                            if (myOriginNodeUser.InnerXml == "AD" && myMethodNodeUser.InnerXml == "DELETE" && routingKey == "user")
                            {
                                Console.WriteLine("Got a message from " + myOriginNodeUser.InnerXml);
                                Console.WriteLine("updating user, and putting it in database");

                                docAlterUser.Load("AlterUser.xml");
                                docAlterUser = xmlDoc;

                                docAlterUser.SelectSingleNode("//user/header/origin").InnerText = "Office";
                                docAlterUser.SelectSingleNode("//user/header/sourceEntityId").InnerText = "";
                                docAlterUser.Save("AlterUser.xml");
                                docAlterUser.Save(Console.Out);



                                Task task = new Task(() => Producer.sendMessage(docAlterUser.InnerXml, "UUID"));
                                task.Start();

                                Console.WriteLine("Origin changed to Office, sending now it to UUID...");
                            }
                            //else if(myOriginNodeUser.InnerXml == "UUID" && myMethodNodeUser.InnerXml == "DELETE" && mySourceIdUser.InnerXml != "" && routingKey == "Office")
                            else if (myOriginNodeUser.InnerXml == "UUID" && myMethodNodeUser.InnerXml == "DELETE" && routingKey == "Office")
                            {
                                Console.WriteLine("Got a delete message from " + mySourceIdUser.InnerXml);
                                Console.WriteLine("The full message from the UUID is: " + xmlDoc.InnerXml);
                                //Console.WriteLine("Source id is: " + myDescription.InnerXml);
                                Console.WriteLine("Deleting user data in database and calendar");

                                try
                                {
                                    using var con = new MySqlConnection(cs);
                                    con.Open();
                                    var sql = "DELETE FROM User WHERE userId= '" + mySourceIdUser.InnerXml + "'";

                                    using var cmd = new MySqlCommand(sql, con);

                                    cmd.ExecuteNonQuery();
                                    Console.WriteLine("User deleted from database");
                                }
                                catch (SqlException e)
                                {
                                    Console.WriteLine("Exception message: " + e.Message);
                                        docAlterLog.Load("AlterLog.xml");

                                        docAlterLog.SelectSingleNode("//log/header/code").InnerText = "5000";
                                        docAlterLog.SelectSingleNode("//log/header/timestamp").InnerText = dateTimeParsedXML;
                                        docAlterLog.SelectSingleNode("//log/body/objectSourceId").InnerText = "Delete User";
                                        docAlterLog.SelectSingleNode("//log/body/description").InnerText = "User deleted error in db line 833";
                                        docAlterLog.Save("AlterLog.xml");
                                        docAlterLog.Save(Console.Out);

                                        Task taskLogAsync1 = new Task(() => Producer.sendMessageLogging(docAlterLog.InnerXml, "logging"));
                                        taskLogAsync1.Start();
                                }

                                    docAlterLog.Load("AlterLog.xml");

                                    docAlterLog.SelectSingleNode("//log/header/code").InnerText = "5000";
                                    docAlterLog.SelectSingleNode("//log/header/timestamp").InnerText = dateTimeParsedXML;
                                    docAlterLog.SelectSingleNode("//log/body/objectSourceId").InnerText = "Delete User";
                                    docAlterLog.SelectSingleNode("//log/body/description").InnerText = "User deleted succesfully";
                                    docAlterLog.Save("AlterLog.xml");
                                    docAlterLog.Save(Console.Out);

                                    Task taskLogAsync = new Task(() => Producer.sendMessageLogging(docAlterLog.InnerXml, "logging"));
                                    taskLogAsync.Start();

                                Console.WriteLine("Deleting user in db sending it to logs(Last step)...");
                            }
                        }
                        else if (xmlValidationSubscribe)
                        {
                            Console.WriteLine("Valid Subscribe XML");

                            //XML head
                            XmlNode myMethodNode = xmlDoc.SelectSingleNode("//method");
                            XmlNode myOriginNode = xmlDoc.SelectSingleNode("//origin");
                            XmlNode mySourceEntityId = xmlDoc.SelectSingleNode("//sourceEntityId");
                            XmlNode mySubVersion = xmlDoc.SelectSingleNode("//version");
                            //XML body
                            XmlNode myUUID = xmlDoc.SelectSingleNode("//eventUUID");
                            XmlNode myEventSourceEntityId = xmlDoc.SelectSingleNode("//eventSourceEntityId");
                            XmlNode myAttendeeUUID = xmlDoc.SelectSingleNode("//attendeeUUID");
                            XmlNode myAttendeeSourceEntityId = xmlDoc.SelectSingleNode("//attendeeSourceEntityId");

                            //Message from Front End, Subscribe to Event, sending it to UUID
                            if (myOriginNode.InnerXml == "FrontEnd" && myMethodNode.InnerXml == "SUBSCRIBE" && myUUID.InnerXml != "" && myAttendeeUUID.InnerXml != "")
                            {

                                Console.WriteLine("Got a message from " + myOriginNode.InnerXml);
                                Console.WriteLine("Updating origin \"" + myOriginNode.InnerXml + "\" to Office");

                                docAlterSub.Load("AlterSubscribe.xml");
                                docAlterSub = xmlDoc;

                                docAlterSub.SelectSingleNode("//eventSubscribe/header/origin").InnerText = "Office";
                                docAlterSub.Save("AlterSubscribe.xml");
                                docAlterSub.Save(Console.Out);


                                Task task = new Task(() => Producer.sendMessage(docAlterSub.InnerXml, "UUID"));
                                task.Start();

                                Console.WriteLine("\nOrigin changed to Office, sending now it to UUID...");

                            }
                            //Message from UUID, Subscribe, setting it in out database
                            else if (myOriginNode.InnerXml == "UUID" && myMethodNode.InnerXml == "SUBSCRIBE" && myEventSourceEntityId.InnerXml != "" && myAttendeeSourceEntityId.InnerXml != "")
                            {
                                    int iNewRowIdentity = -1;
                                Console.WriteLine("Got a message from " + myOriginNode.InnerXml);
                                Console.WriteLine("Subscribing data in database and calendar");

                                using var con = new MySqlConnection(cs);
                                con.Open();
                                string email = "";
                                string name = "";
                                string eventId = "";
                                string eventName = "";
                                string startTime = "";
                                string endTime = "";
                                string location = "";
                                string description = "";

                                var sqlForId = "SELECT * FROM Event WHERE eventId = '" + myEventSourceEntityId.InnerXml + "'";
                                using var cmdx = new MySqlCommand(sqlForId, con);
                                MySqlDataReader drx = cmdx.ExecuteReader();
                                 if (drx.Read())
                                   {
                                        Console.WriteLine(drx.ToString());
                                        eventId = drx[0].ToString();
                                        eventName = drx[3].ToString();
                                        //startTime = drx[4].ToString().Substring(0, (drx[4].ToString().Length - 6));
                                        //endTime = drx[5].ToString().Substring(0, (drx[5].ToString().Length - 6));
                                        startTime = drx[4].ToString();
                                        endTime = drx[5].ToString();
                                        location = drx[7].ToString();
                                        description = drx[6].ToString();
                                        Console.WriteLine(eventId);
                                   }
                                  drx.Close();

                                var sqlForEmail = "SELECT email FROM User WHERE userId = '" + myAttendeeSourceEntityId.InnerXml + "'";
                                using var cmd1 = new MySqlCommand(sqlForEmail, con);
                                MySqlDataReader dr = cmd1.ExecuteReader();

                                if (dr.Read())
                                  {
                                    email = dr[0].ToString().Remove(dr[0].ToString().Length - 24);
                                    email = $"{email}@flowupdesiderius.onmicrosoft.com";
                                    Console.WriteLine(email);
                                  }
                                    dr.Close();

                                var sqlForFirstname = "SELECT firstname FROM User WHERE userId = '" + myAttendeeSourceEntityId.InnerXml + "'";
                                using var cmd2 = new MySqlCommand(sqlForFirstname, con);
                                MySqlDataReader dr1 = cmd2.ExecuteReader();
                                    if (dr1.Read())
                                    {
                                        name = dr1[0].ToString();
                                        Console.WriteLine(name);
                                    }
                                    dr1.Close();

                                    var sqlForLastname = "SELECT lastname FROM User WHERE userId = '" + myAttendeeSourceEntityId.InnerXml + "'";
                                    using var cmd3 = new MySqlCommand(sqlForLastname, con);
                                    MySqlDataReader dr2 = cmd3.ExecuteReader();

                                    if (dr2.Read())
                                    {
                                        name += String.Join(" ", dr2[0].ToString());
                                        Console.WriteLine(name);
                                    }
                                    dr2.Close();

                                    try
                                    {
                                        List<Attendee> attendeesAtCreate = new List<Attendee>();
                                        Program.RunAsync("create", eventName, description, startTime, endTime,
                                        location, attendeesAtCreate, name, email, true, eventId).GetAwaiter().GetResult();

                                    }
                                    catch (Exception ex)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine(ex.Message);
                                        Console.ResetColor();
                                        docAlterLog.Load("AlterLog.xml");

                                        docAlterLog.SelectSingleNode("//log/header/code").InnerText = "3000";
                                        docAlterLog.SelectSingleNode("//log/header/timestamp").InnerText = dateTimeParsedXML;
                                        docAlterLog.SelectSingleNode("//log/body/objectSourceId").InnerText = "Subscribe";
                                        docAlterLog.SelectSingleNode("//log/body/description").InnerText = "Subscribe error Async db line 976";
                                        docAlterLog.Save("AlterLog.xml");
                                        docAlterLog.Save(Console.Out);

                                        Task taskLogAsync1 = new Task(() => Producer.sendMessageLogging(docAlterLog.InnerXml, "logging"));
                                        taskLogAsync1.Start();
                                    }

                                    var sql = "INSERT INTO Subscribe(userId, eventId) VALUES(@userId, @eventId); SELECT @@IDENTITY";
                                    try
                                    {
                                        using var cmd = new MySqlCommand(sql, con);


                                        cmd.Parameters.AddWithValue("@eventId", myEventSourceEntityId.InnerXml);
                                        cmd.Parameters.AddWithValue("@userId", myAttendeeSourceEntityId.InnerXml);
                                        //cmd.Prepare();
                                        //cmd.ExecuteNonQuery();
                                        iNewRowIdentity = Convert.ToInt32(cmd.ExecuteScalar());
                                        Console.WriteLine("Event Id in database is: " + iNewRowIdentity);
                                    }
                                    catch (SqlException e)
                                    {
                                        Console.WriteLine(e.Message);
                                        docAlterLog.Load("AlterLog.xml");

                                        docAlterLog.SelectSingleNode("//log/header/code").InnerText = "3000";
                                        docAlterLog.SelectSingleNode("//log/header/timestamp").InnerText = dateTimeParsedXML;
                                        docAlterLog.SelectSingleNode("//log/body/objectSourceId").InnerText = "Subscribe";
                                        docAlterLog.SelectSingleNode("//log/body/description").InnerText = "Subscribe error db line 1005";
                                        docAlterLog.Save("AlterLog.xml");
                                        docAlterLog.Save(Console.Out);

                                        Task taskLogAsync1 = new Task(() => Producer.sendMessageLogging(docAlterLog.InnerXml, "logging"));
                                        taskLogAsync1.Start();
                                    }
                                    Console.WriteLine("Subscribe inserted in database");

                                docAlterSub.Load("AlterSubscribe.xml");
                                docAlterSub = xmlDoc;

                                docAlterSub.SelectSingleNode("//eventSubscribe/header/origin").InnerText = "Office";
                                docAlterSub.SelectSingleNode("//eventSubscribe/header/sourceEntityId").InnerText = iNewRowIdentity.ToString();
                                docAlterSub.Save("AlterSubscribe.xml");

                                docAlterSub.Save(Console.Out);

                                Task task = new Task(() => Producer.sendMessage(docAlterSub.InnerXml, "UUID"));
                                task.Start();

                                    docAlterLog.Load("AlterLog.xml");

                                    docAlterLog.SelectSingleNode("//log/header/code").InnerText = "3000";
                                    docAlterLog.SelectSingleNode("//log/header/timestamp").InnerText = dateTimeParsedXML;
                                    docAlterLog.SelectSingleNode("//log/body/objectSourceId").InnerText = "Subscribe";
                                    docAlterLog.SelectSingleNode("//log/body/description").InnerText = "Subscribe succesfully";
                                    docAlterLog.Save("AlterLog.xml");
                                    docAlterLog.Save(Console.Out);

                                    Task taskLogAsync = new Task(() => Producer.sendMessageLogging(docAlterLog.InnerXml, "logging"));
                                    taskLogAsync.Start();

                                    Console.WriteLine("\nSending subscribe message to UUID with Office Subscribe entity Id and logging(Last step)");

                            }
                            //Unubscribe from Front end, change origin and send to UUID
                            if (myOriginNode.InnerXml == "FrontEnd" && myMethodNode.InnerXml == "UNSUBSCRIBE" && myUUID.InnerXml != "" && myAttendeeUUID.InnerXml != "")
                            {
                                Console.WriteLine("Unsusbscribing from event, and putting it in database");

                                Console.WriteLine("Got a message from " + myOriginNode.InnerXml);
                                Console.WriteLine("Updating origin from \"" + myOriginNode.InnerXml + " to \"Office\"");


                                docAlterSub.Load("AlterSubscribe.xml");
                                docAlterSub = xmlDoc;

                                docAlterSub.SelectSingleNode("//eventSubscribe/header/origin").InnerText = "Office";
                                docAlterSub.Save("AlterSubscribe.xml");
                                docAlterSub.Save(Console.Out);

                                Task task = new Task(() => Producer.sendMessage(docAlterSub.InnerXml, "UUID"));
                                task.Start();

                                Console.WriteLine("Origin changed to Office, sending delete now it to UUID...");
                            }
                            //Unubscribe from UUID, change it in db
                            else if (myOriginNode.InnerXml == "UUID" && myMethodNode.InnerXml == "UNSUBSCRIBE" && myEventSourceEntityId.InnerXml != "" && myAttendeeSourceEntityId.InnerXml != "")
                            {
                                Console.WriteLine("Unsusbscribing from event, and putting it in database");

                                Console.WriteLine("Got a subscribe message from " + myOriginNode.InnerXml);
                                Console.WriteLine("The full message from the UUID is: " + xmlDoc.InnerXml);
                                Console.WriteLine("Deleting event data in database and calendar");

                                using var con = new MySqlConnection(cs);
                                con.Open();
                                    var sqlForId = "SELECT graphResponse FROM Event WHERE eventId = '" + myEventSourceEntityId.InnerXml + "'";
                                    using var cmd1 = new MySqlCommand(sqlForId, con);
                                    MySqlDataReader dr = cmd1.ExecuteReader();
                                    string eventId = "";
                                    string userEmail = "";
                                    if (dr.Read())
                                    {
                                        eventId = dr[0].ToString();
                                        Console.WriteLine(eventId);
                                    }
                                    dr.Close();

                                    var sqlForEmail = "SELECT email FROM User WHERE userId = '" + myAttendeeSourceEntityId.InnerXml + "'";
                                    using var cmd2 = new MySqlCommand(sqlForEmail, con);
                                    MySqlDataReader dr2 = cmd2.ExecuteReader();
                                    if (dr2.Read())
                                    {
                                    userEmail = dr2[0].ToString().Remove(dr2[0].ToString().Length - 24);
                                    userEmail = $"{userEmail}@flowupdesiderius.onmicrosoft.com";
                                       }
                                    dr2.Close();


                                    try
                                    {
                                        List<Attendee> attendeesAtCreate = new List<Attendee>();
                                        Program.RunAsync("update", "", "", "", "",
                                        "", attendeesAtCreate, userEmail, "", true, eventId).GetAwaiter().GetResult();

                                    }
                                    catch (Exception ex)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine(ex.Message);
                                        Console.ResetColor();
                                        docAlterLog.Load("AlterLog.xml");

                                        docAlterLog.SelectSingleNode("//log/header/code").InnerText = "5000";
                                        docAlterLog.SelectSingleNode("//log/header/timestamp").InnerText = dateTimeParsedXML;
                                        docAlterLog.SelectSingleNode("//log/body/objectSourceId").InnerText = "Unsubscribe";
                                        docAlterLog.SelectSingleNode("//log/body/description").InnerText = "Unsubscribe error RunAsync line 1113";
                                        docAlterLog.Save("AlterLog.xml");
                                        docAlterLog.Save(Console.Out);

                                        Task taskLogAsync1 = new Task(() => Producer.sendMessageLogging(docAlterLog.InnerXml, "logging"));
                                        taskLogAsync1.Start();
                                    }

                                    var sql = "DELETE FROM Subscribe WHERE eventId = '" + myEventSourceEntityId.InnerXml + "' AND userId = '" + myAttendeeSourceEntityId.InnerXml + "'";
                                using var cmd = new MySqlCommand(sql, con);

                                //cmd.Prepare();
                                cmd.ExecuteNonQuery();

                                    docAlterLog.Load("AlterLog.xml");

                                    docAlterLog.SelectSingleNode("//log/header/code").InnerText = "5000";
                                    docAlterLog.SelectSingleNode("//log/header/timestamp").InnerText = dateTimeParsedXML;
                                    docAlterLog.SelectSingleNode("//log/body/objectSourceId").InnerText = "Unsubscribe";
                                    docAlterLog.SelectSingleNode("//log/body/description").InnerText = "Unsubscribe succesfully";
                                    docAlterLog.Save("AlterLog.xml");
                                    docAlterLog.Save(Console.Out);

                                    Task taskLogAsync = new Task(() => Producer.sendMessageLogging(docAlterLog.InnerXml, "logging"));
                                    taskLogAsync.Start();

                                    Console.WriteLine("Event deleted in database and logged(Last step)");
                            }
                        }
                        //XML error from UUID
                        else if(xmlValidationError)
                        {
                            //header
                            XmlNode myCodeNode = xmlDoc.SelectSingleNode("//code");
                            XmlNode myOriginNode = xmlDoc.SelectSingleNode("//origin");
                            XmlNode myTimestamp = xmlDoc.SelectSingleNode("//timestamp");
                            //body
                            XmlNode myobjectUUID = xmlDoc.SelectSingleNode("//objectUUID");
                            XmlNode myobjectSourceId = xmlDoc.SelectSingleNode("//objectSourceId");
                            XmlNode myObjectOrigin = xmlDoc.SelectSingleNode("//objectOrigin");
                            XmlNode myErrorMessage = xmlDoc.SelectSingleNode("//description");

                            Console.WriteLine("Error XML received");
                            Console.WriteLine("Message is: " + myErrorMessage.InnerXml);


                            docAlterError.Load("AlterError.xml");
                            docAlterError = xmlDoc;

                            docAlterError.SelectSingleNode("//error/header/origin").InnerText = "Office";
                            docAlterError.SelectSingleNode("//error/header/timestamp").InnerText = dateTimeParsedXML;
                            docAlterError.Save("Alter.xml");
                            docAlterError.Save(Console.Out);

                            string erroXMLWithoutVersion = docAlterError.InnerXml.Substring(55);
                            Console.WriteLine(erroXMLWithoutVersion);

                            Task task = new Task(() => Producer.sendMessageLogging(docAlterError.InnerXml, "logging"));
                            task.Start();

                        }
                        else
                            {
                                Console.WriteLine("XML was wrong, sending it to logging");
                                Task task = new Task(() => Producer.sendMessageLogging(message, "logging"));
                                task.Start();
                            }

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Weird message came in: " + message);
                    }

                };
                channel.BasicConsume(queue: queueName,
                autoAck: true,
                consumer: consumer);

                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();
            }

            }
            catch (IOException e)
            {
                Console.WriteLine("cs file could not be read:");
                Console.WriteLine(e.Message);
            }

        }
    }
}
