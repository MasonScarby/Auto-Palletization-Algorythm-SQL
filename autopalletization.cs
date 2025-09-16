



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using DevExpress.XtraExport.Implementation;
using PalletMaster.Controls;
using System.Data.SqlTypes;
using System.Windows.Forms;
using System.Diagnostics.Eventing.Reader;

namespace PalletMaster.Util
{
    class AutoPalletize
    {
        public static int Populate(string sqlCommand)
        {
            int retvalue = 0;
            object returnval;
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.SysproCompany1ConnectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(sqlCommand, connection);

                returnval = command.ExecuteScalar();
            }
            if (returnval != DBNull.Value )
            {
                retvalue = Convert.ToInt32(returnval);
                return retvalue;
            }
            else
                return retvalue;
        }


        public static int customerNumber;
        public static string salesorder;
        public static int currentPalletNumber = 0;
        public static int nextPalletLineNumber = 1;
         
        DataSets.PalletData.PossibleStockCodesDataTable tempLockedPallets = new DataSets.PalletData.PossibleStockCodesDataTable();

        public static List<DataSets.PalletData.PossibleStockCodesDataTable> AutoPalletizeShipment(DataSets.PalletData.PossibleStockCodesDataTable dataTableToPalletize)
        {
            

            
            List<DataSets.PalletData.PossibleStockCodesDataTable> returnList = new List<DataSets.PalletData.PossibleStockCodesDataTable>();
            DataSets.PalletData.PossibleStockCodesDataTable tempLockedPallets = new DataSets.PalletData.PossibleStockCodesDataTable();

            string sortOrder = "SecondaryGrouping,PrimaryGrouping,TotalSpaceNeeded Desc";

            DataView dv = dataTableToPalletize.DefaultView;
            dv.Sort = sortOrder;
            DataTable dt = dv.ToTable();
            salesorder = string.Empty;
            if (dt.Rows.Count > 0)
            {
                salesorder = dt.Rows[0]["SalesOrder"].ToString();
            }
            //organized the list of products by production schedule
            ConvertToStronglyTypedDataTable(dt, out dataTableToPalletize);
 
            int Results = Populate("select max(PalletNumber) from MISCO_xPalletDetail where SalesOrder =" + salesorder + " and Locked = 1");
            if (Results > 0)
            {
                currentPalletNumber = Results;
                nextPalletLineNumber++;
            }
            currentPalletNumber++;


            customerNumber = Populate("select Customer from SorMaster where SalesOrder =" + salesorder);

            //#region GetFullPallets

            if (customerNumber != 10457)
            {
                for (int i = 0; i < dataTableToPalletize.Rows.Count;)
                {
                    if (dataTableToPalletize.Rows.Count == 0)
                    {
                        break;
                    }

                    decimal totalQty = (decimal)dataTableToPalletize.Rows[i]["TotalQty"];
                    decimal palletPercentageForOne = (decimal)dataTableToPalletize.Rows[i]["PalletPercentageForOne"];
                    string description = dataTableToPalletize.Rows[i]["Description"].ToString();
                    string productionSchedule = dataTableToPalletize.Rows[i]["ProductionSchedule"].ToString().Trim();
                    string stockCode = dataTableToPalletize.Rows[i]["StockCode"].ToString();
 
                    decimal maxPerPallet = 48m;
 
                    if ((totalQty * palletPercentageForOne * 100 >= 99.99m))
                    {
                        if (palletPercentageForOne != 0)
                        {
                            maxPerPallet = Math.Round(1 / palletPercentageForOne, 0);
                        }

                        if (totalQty > maxPerPallet)
                        {
                            DataRow newRow = dataTableToPalletize.NewRow();
                            newRow.ItemArray = dataTableToPalletize.Rows[i].ItemArray.Clone() as object[];
                            newRow["TotalQty"] = totalQty - maxPerPallet;
                            newRow["PalletLineNumber"] = nextPalletLineNumber + 1;
                            dataTableToPalletize.Rows.Add(newRow);

                            dataTableToPalletize.Rows[i]["TotalQty"] = maxPerPallet;
                            dataTableToPalletize.Rows[i]["PalletLineNumber"] = nextPalletLineNumber;
                            nextPalletLineNumber++;
                        }

                        DataSets.PalletData.PossibleStockCodesDataTable onePalletDt = new DataSets.PalletData.PossibleStockCodesDataTable();
                        dataTableToPalletize.Rows[i]["PalletNumber"] = currentPalletNumber;
                        onePalletDt.ImportRow(dataTableToPalletize.Rows[i]);

                        dataTableToPalletize.Rows.RemoveAt(i);
                        returnList.Add(onePalletDt);
                        currentPalletNumber++;
                    }
                    else if (totalQty == 0)
                    {
                        dataTableToPalletize.Rows.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
                
            }
            else
            {
                for (int i = 0; i < dataTableToPalletize.Rows.Count;)
                {
                    if (dataTableToPalletize.Rows.Count == 0)
                    {
                        break;
                    }

                    decimal totalQty = (decimal)dataTableToPalletize.Rows[i]["TotalQty"];
                    decimal palletPercentageForOne = (decimal)dataTableToPalletize.Rows[i]["PalletPercentageForOne"];
                    string description = dataTableToPalletize.Rows[i]["Description"].ToString();
                    string productionSchedule = dataTableToPalletize.Rows[i]["ProductionSchedule"].ToString().Trim();
                    string stockCode = dataTableToPalletize.Rows[i]["StockCode"].ToString();

                    bool[] isMPC = { description.StartsWith("MPC"), description.StartsWith("elements"), description.StartsWith("Majestic") };

                    //bool isPrivateLabel = description.StartsWith("")
                    //MPC IS any desciption with element - MPC - or Majestic at begining
                    //Private is any description that does have that
                    decimal maxPerPallet = 48m;

                    if (customerNumber == 10457)
                    {

                        if (productionSchedule == "Round Quart" && totalQty >= 20)
                        {
                            if (totalQty == 50)
                            {
                                // Create first pallet with 20
                                DataRow pallet20 = tempLockedPallets.NewRow();
                                pallet20.ItemArray = dataTableToPalletize.Rows[i].ItemArray.Clone() as object[];
                                pallet20["TotalQty"] = 20m;
                                pallet20["PalletNumber"] = currentPalletNumber++;
                                pallet20["PalletLineNumber"] = nextPalletLineNumber++;
                                tempLockedPallets.Rows.Add(pallet20);

                                // Create second pallet with 30
                                DataRow pallet30 = tempLockedPallets.NewRow();
                                pallet30.ItemArray = dataTableToPalletize.Rows[i].ItemArray.Clone() as object[];
                                pallet30["TotalQty"] = 30m;
                                pallet30["PalletNumber"] = currentPalletNumber++;
                                pallet30["PalletLineNumber"] = nextPalletLineNumber++;
                                tempLockedPallets.Rows.Add(pallet30);

                                dataTableToPalletize.Rows.RemoveAt(i);
                                continue;
                            }

                            // If it's not MPC
                            if (!isMPC.Contains(true))
                            {
                                // Create a single pallet of 20 for non-MPC products
                                DataRow pallet20 = tempLockedPallets.NewRow();
                                pallet20.ItemArray = dataTableToPalletize.Rows[i].ItemArray.Clone() as object[];
                                pallet20["TotalQty"] = 20m;
                                pallet20["PalletNumber"] = currentPalletNumber++;
                                pallet20["PalletLineNumber"] = nextPalletLineNumber++;
                                tempLockedPallets.Rows.Add(pallet20);

                                dataTableToPalletize.Rows.RemoveAt(i);
                                continue;
                            }


                        }
                        if (!isMPC.Contains(true) && productionSchedule.Trim() == "F-Style Gallons")
                        {


                            // If TotalQty is 15, create a pallet with 15
                            if (totalQty == 15m)
                            {
                                DataRow pallet15 = tempLockedPallets.NewRow();
                                pallet15.ItemArray = dataTableToPalletize.Rows[i].ItemArray.Clone() as object[];
                                pallet15["TotalQty"] = 15m;
                                pallet15["PalletNumber"] = currentPalletNumber++;
                                pallet15["PalletLineNumber"] = nextPalletLineNumber++;
                                tempLockedPallets.Rows.Add(pallet15);

                                dataTableToPalletize.Rows.RemoveAt(i);
                                continue;
                            }
                        }





                        // Handle 2.5Gallon production schedule and specific total quantity
                        if (productionSchedule == "2.5 Gallon" && totalQty >= 12)
                        {
                            decimal splitSize = 24m; // Try to fill pallets of 24
                            int fullPalletCount = (int)(totalQty / splitSize);
                            decimal remainder = totalQty % splitSize;

                            // Create full 24-unit pallets
                            for (int j = 0; j < fullPalletCount; j++)
                            {
                                DataRow newPallet = tempLockedPallets.NewRow();
                                newPallet.ItemArray = dataTableToPalletize.Rows[i].ItemArray.Clone() as object[];
                                newPallet["TotalQty"] = splitSize;
                                newPallet["PalletNumber"] = currentPalletNumber++;
                                newPallet["PalletLineNumber"] = nextPalletLineNumber++;
                                tempLockedPallets.Rows.Add(newPallet);
 
                            }

                            // Create a final pallet if there's a leftover quantity (12 or more)
                            if (remainder >= 10)
                            {
                                DataRow remainderPallet = tempLockedPallets.NewRow();
                                remainderPallet.ItemArray = dataTableToPalletize.Rows[i].ItemArray.Clone() as object[];
                                remainderPallet["TotalQty"] = remainder;
                                remainderPallet["PalletNumber"] = currentPalletNumber++;
                                remainderPallet["PalletLineNumber"] = nextPalletLineNumber++;
                                tempLockedPallets.Rows.Add(remainderPallet);
                            }

                            dataTableToPalletize.Rows.RemoveAt(i);
                            continue;
                        }


                        if (productionSchedule == "Round Gallons")
                        {
                            if (totalQty > 10 && totalQty < 35)
                            {
                                // Create a new pallet using the actual quantity in the row.
                                DataRow newPallet = tempLockedPallets.NewRow();
                                newPallet.ItemArray = dataTableToPalletize.Rows[i].ItemArray.Clone() as object[];
                                newPallet["TotalQty"] = totalQty;  // Use the actual amount available
                                newPallet["PalletNumber"] = currentPalletNumber++;
                                newPallet["PalletLineNumber"] = nextPalletLineNumber++;
                                tempLockedPallets.Rows.Add(newPallet);

                                dataTableToPalletize.Rows.RemoveAt(i);
                                continue;
                            }
                            if (totalQty == 36)
                            {
                                // Create a new pallet using the actual quantity in the row.
                                DataRow newPallet = tempLockedPallets.NewRow();
                                newPallet.ItemArray = dataTableToPalletize.Rows[i].ItemArray.Clone() as object[];
                                newPallet["TotalQty"] = totalQty;  // Use the actual amount available
                                newPallet["PalletNumber"] = currentPalletNumber++;
                                newPallet["PalletLineNumber"] = nextPalletLineNumber++;
                                tempLockedPallets.Rows.Add(newPallet);

                                dataTableToPalletize.Rows.RemoveAt(i);
                                continue;
                            }
                        }



                        if (productionSchedule == "Ounces" && totalQty >= 10 && totalQty < 24)
                        {
                            // Create a new pallet using the actual quantity in the row.
                            DataRow newPallet = tempLockedPallets.NewRow();
                            newPallet.ItemArray = dataTableToPalletize.Rows[i].ItemArray.Clone() as object[];
                            newPallet["TotalQty"] = totalQty;  // Use the actual amount available
                            newPallet["PalletNumber"] = currentPalletNumber++;
                            newPallet["PalletLineNumber"] = nextPalletLineNumber++;
                            tempLockedPallets.Rows.Add(newPallet);

                            dataTableToPalletize.Rows.RemoveAt(i);
                            continue;
                        }

                        // Handle MPC (or any other conditions related to MPC)
                        if (isMPC.Contains(true) && totalQty == 52)
                        {
                            decimal splitSize = 26m; // Split into two pallets of 26 each
                            int numPallets = 2;

                            for (int j = 0; j < numPallets; j++)
                            {
                                DataRow newPallet = tempLockedPallets.NewRow();
                                newPallet.ItemArray = dataTableToPalletize.Rows[i].ItemArray.Clone() as object[];
                                newPallet["TotalQty"] = splitSize;
                                newPallet["PalletNumber"] = currentPalletNumber++;
                                newPallet["PalletLineNumber"] = nextPalletLineNumber++;
                                tempLockedPallets.Rows.Add(newPallet);
                            }

                            dataTableToPalletize.Rows.RemoveAt(i);
                            continue;
                        }

                        if (isMPC.Contains(true))
                        {

                            if (totalQty % 13 == 0)
                            {
                                maxPerPallet = 39m;
                            }
                            else if (totalQty % 12 == 0)
                            {
                                maxPerPallet = 48m;
                            }
                        }
                    }

                    //end of specifications for Singer
                    if ((totalQty * palletPercentageForOne * 100 >= 99.99m))
                    {
                        if (palletPercentageForOne != 0)
                        {
                            maxPerPallet = Math.Round(1 / palletPercentageForOne, 0);
                        }

                        if (totalQty > maxPerPallet)
                        {
                            DataRow newRow = dataTableToPalletize.NewRow();
                            newRow.ItemArray = dataTableToPalletize.Rows[i].ItemArray.Clone() as object[];
                            newRow["TotalQty"] = totalQty - maxPerPallet;
                            newRow["PalletLineNumber"] = nextPalletLineNumber + 1;
                            dataTableToPalletize.Rows.Add(newRow);

                            dataTableToPalletize.Rows[i]["TotalQty"] = maxPerPallet;
                            dataTableToPalletize.Rows[i]["PalletLineNumber"] = nextPalletLineNumber;
                            nextPalletLineNumber++;
                        }

                        DataSets.PalletData.PossibleStockCodesDataTable onePalletDt = new DataSets.PalletData.PossibleStockCodesDataTable();
                        dataTableToPalletize.Rows[i]["PalletNumber"] = currentPalletNumber;
                        onePalletDt.ImportRow(dataTableToPalletize.Rows[i]);

                        dataTableToPalletize.Rows.RemoveAt(i);
                        returnList.Add(onePalletDt);
                        currentPalletNumber++;
                    }
                    else if (totalQty == 0)
                    {
                        dataTableToPalletize.Rows.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }


            }
 
         //EMD FOR LOOP

            if (nextPalletLineNumber != 1)
            {
                nextPalletLineNumber++;
            }

            string _Where = "SecondaryGrouping";
            string _OrderBy = "PrimaryGrouping";
 

            if (customerNumber == 10457)
            {
                //dont want to run ProcessLineItemsToPallets
            }
            else
            {
                ProcessLineItemsToPallets(dataTableToPalletize, _Where, _OrderBy, ref currentPalletNumber, ref nextPalletLineNumber, returnList);
            }
            LoadSecondaryGroupsToPallets(dataTableToPalletize, _Where, _OrderBy, returnList, currentPalletNumber, nextPalletLineNumber);

            // Handle remaining items in dataTableToPalletize
            while (dataTableToPalletize.Rows.Count > 0)
            {
                DataSets.PalletData.PossibleStockCodesDataTable newDataTable = new DataSets.PalletData.PossibleStockCodesDataTable();

                for (int i = 0; i < dataTableToPalletize.Rows.Count;)
                {
                    decimal totalQty = (decimal)dataTableToPalletize.Rows[i]["TotalQty"];
                    decimal palletPercentageForOne = (decimal)dataTableToPalletize.Rows[i]["PalletPercentageForOne"];

                    if (totalQty == 0)
                    {
                        dataTableToPalletize.Rows.RemoveAt(i);
                        continue;
                    }

                    if ((totalQty * palletPercentageForOne * 100) + CalculatePalletFilledPercentage(newDataTable) >= 100.0m)
                    {
                        continue;
                    }
                    else
                    {
                        dataTableToPalletize.Rows[i]["PalletLineNumber"] = nextPalletLineNumber;
                        nextPalletLineNumber++;
                        dataTableToPalletize.Rows[i]["PalletNumber"] = currentPalletNumber;
                        newDataTable.ImportRow(dataTableToPalletize.Rows[i]);
                        dataTableToPalletize.Rows.RemoveAt(i);
                    }
                }
                returnList.Add(newDataTable);
                currentPalletNumber++;
            }

            // Ensure locked pallets get unique pallet numbers

            if(customerNumber == 10457)
            {
                int highestPalletNumber = returnList.Any()
              ? returnList.Max(pallet => pallet.Rows.Cast<DataRow>().Max(row => row.Field<int>("PalletNumber")))
              : currentPalletNumber;

                for (int i = 0; i < tempLockedPallets.Rows.Count; i++)
                {
                    DataSets.PalletData.PossibleStockCodesDataTable newPalletTable = new DataSets.PalletData.PossibleStockCodesDataTable();

                    DataRow newRow = newPalletTable.NewRow();
                    newRow.ItemArray = tempLockedPallets.Rows[i].ItemArray.Clone() as object[];
                    newRow["PalletLineNumber"] = nextPalletLineNumber++;

                    // Assign a unique pallet number that does not overlap with existing ones
                    highestPalletNumber++;
                    newRow["PalletNumber"] = highestPalletNumber;

                    newPalletTable.Rows.Add(newRow);
                    returnList.Add(newPalletTable);
                }
            }
          

         
            currentPalletNumber = 0;
            nextPalletLineNumber = 1;
            return returnList;
        }




        /// <summary>
        /// Gets Groups by "Where" to see if items fill a pallet
        /// </summary>
        /// <param name="dataTableToPalletize">line items for the order.</param>
        /// <param name="_Where">Master Grouping - Secondary</param>
        /// <param name="_OrderBy">Internal Grouping - Primary</param>
        /// <param name="currentPalletNumber">Passed in to mimic current process.</param>
        /// <param name="nextPalletLineNumber">Passed in to mimic current process.</param>
        /// <param name="returnList">list of pallets that have been configured and those appended by this function.</param>
        /// 


        //Primary - first sorting into pallet function, by distinct value... production schedule in this case.
        private static void ProcessLineItemsToPallets(DataSets.PalletData.PossibleStockCodesDataTable dataTableToPalletize, string _Where, string _OrderBy, ref int currentPalletNumber, ref int nextPalletLineNumber, List<DataSets.PalletData.PossibleStockCodesDataTable> returnList)
        {
            // The GetDistinct returns new datatable, so this loop is not an issue on maintenance after row deletion.
            foreach (System.Data.DataRow dr in GetDistinctValuesInColumn(dataTableToPalletize, _Where).Rows)
            {
                // Get SubSet of Data to work with - Secondary Grouping
                DataRow[] drs = dataTableToPalletize.Select(string.Format("{0}='{1}'", _Where, dr["Distinct"].ToString()), _OrderBy);

                while (drs.Length > 0 && CalculatePalletFilledPercentage(CopyToStronglyTypedDataTable(drs.CopyToDataTable())) > 100)
                {
                    DataSets.PalletData.PossibleStockCodesDataTable newDataTable = new DataSets.PalletData.PossibleStockCodesDataTable();

                    for (int i = 0; i < drs.Count(); i++)
                    {
                        if (drs.Count().Equals(0)) { break; }

                        decimal totalQty = (decimal)drs[i]["TotalQty"];
                        decimal palletPercentageForOne = (decimal)drs[i]["PalletPercentageForOne"];

                        if (totalQty == 0)
                        {
                            drs[i].Delete(); // Just marks for deletion - we'll cleanup.
                            continue;
                        }

                        // Check if item fits before adding it
                        if (Math.Round((totalQty * palletPercentageForOne * 100) + CalculatePalletFilledPercentage(newDataTable), 4) <= 100.0m)
                        {
                            drs[i]["PalletLineNumber"] = nextPalletLineNumber;
                            nextPalletLineNumber++;

                            drs[i]["PalletNumber"] = currentPalletNumber; // ✅ Always use currentPalletNumber
                            newDataTable.ImportRow(drs[i]);

                            drs[i].Delete(); // Just marks for deletion - we'll cleanup.
                        }
                    }

                    if (newDataTable.Rows.Count > 0)
                    {
                        returnList.Add(newDataTable);
                        currentPalletNumber++; // ✅ Only increment AFTER a pallet is created
                    }

                    CleanUpDeletedRow(dataTableToPalletize);
                    drs = dataTableToPalletize.Select(string.Format("{0}='{1}'", _Where, dr["Distinct"].ToString()), _OrderBy);
                }
            }
        }






        /// <summary>
        /// Rotates through the current pallets to see if the "secondary" group can fit.
        /// If so, it appends the items to the pallet.
        /// If not, it creates a new pallet for the group.
        /// </summary>
        /// <param name="dataTableToPalletize"></param>
        /// <param name="_Where">Should be the secondary grouping</param>
        /// <param name="_OrderBy">Should be the primary grouping</param>
        /// <param name="returnList">current list of pallets to be maintained</param>
        /// 



        //Secondary - first sorting into pallet function, by distinct value... production schedule in this case.

        private static void LoadSecondaryGroupsToPallets(DataSets.PalletData.PossibleStockCodesDataTable dataTableToPalletize, string _Where, string _OrderBy, List<DataSets.PalletData.PossibleStockCodesDataTable> returnList, int currentPalletNumber1, int nextPalletLineNumber1)
        {

            List<DataRow> mpcRows = new List<DataRow>();
            List<DataRow> nonMpcRows = new List<DataRow>();

            // Split the rows into MPC and non-MPC based on description for customerNumber 10457
            if (customerNumber == 10457)
            {
                foreach (DataRow row in dataTableToPalletize.Rows)
                {
                    string description = row["Description"].ToString().ToUpper();
                    bool isMPC = description.StartsWith("MPC") || description.StartsWith("ELEMENTS") || description.StartsWith("MAJESTIC");

                    if (isMPC)
                    {
                        mpcRows.Add(row);
                    }
                    else
                    {
                        nonMpcRows.Add(row);
                    }
                }

                // Process rows for customerNumber 10457
                ProcessRowsToPallets(mpcRows, returnList, ref currentPalletNumber1, ref nextPalletLineNumber1); // Process MPC items first
                ProcessRowsToPallets(nonMpcRows, returnList, ref currentPalletNumber1, ref nextPalletLineNumber1); // Process non-MPC items next
            }
            else
            {
                // For all other customers, process all rows together (non-MPC and MPC combined)
                foreach (DataRow row in dataTableToPalletize.Rows)
                {
                    nonMpcRows.Add(row); // Treat all as non-MPC for other customers
                }

                // Process rows for non-MPC items only
                ProcessRowsToPallets(nonMpcRows, returnList, ref currentPalletNumber1, ref nextPalletLineNumber1);
            }
        }

        private static void ProcessRowsToPallets(List<DataRow> rows, List<DataSets.PalletData.PossibleStockCodesDataTable> returnList, ref int currentPalletNumber, ref int nextPalletLineNumber)
        {
            bool fitsOnPallet = false;

            // First, check all existing pallets to see if they can fit the new rows
            foreach (DataRow row in rows)
            {
                // Check the fill percentage of the current pallets and decide if the row fits
                decimal rowTotalQty = (decimal)row["TotalQty"];
                decimal rowPalletPercentage = (decimal)row["PalletPercentageForOne"];

                bool rowFits = false;

                foreach (var pallet in returnList)
                {
                    // Calculate current fill percentage of the pallet
                    decimal currentFillPercentage = CalculatePalletFilledPercentage(pallet);

                    // Check if adding this row will not exceed the 100% capacity
                    if (Math.Round((rowTotalQty * rowPalletPercentage * 100) + currentFillPercentage, 4) <= 100)
                    {
                        // If it fits, add it to the pallet
                        row["PalletLineNumber"] = nextPalletLineNumber++;
                        row["PalletNumber"] = currentPalletNumber;
                        pallet.ImportRow(row); // Import the row into the current pallet
                        row.Delete(); // Mark the row as processed
                        rowFits = true;
                        break;
                    }
                }

                // If the row doesn't fit on any existing pallet, create a new pallet
                if (!rowFits)
                {
                    // Create a new pallet
                    DataSets.PalletData.PossibleStockCodesDataTable newDataTable = new DataSets.PalletData.PossibleStockCodesDataTable();
                    returnList.Add(newDataTable);
                    currentPalletNumber++; // Increment the pallet number

                    // Add the row to the new pallet
                    row["PalletLineNumber"] = nextPalletLineNumber++;
                    row["PalletNumber"] = currentPalletNumber;
                    newDataTable.ImportRow(row);
                    row.Delete(); // Mark the row as processed
                }
            }
        }






        //originial
        //#region GetFullPallets
        //for (int i = 0; i < dataTableToPalletize.Rows.Count;)
        //{
        //    if (dataTableToPalletize.Rows.Count == 0)
        //    {
        //        break;
        //    }

        //    decimal totalQty = (decimal)dataTableToPalletize.Rows[i]["TotalQty"];
        //    decimal palletPercentageForOne = (decimal)dataTableToPalletize.Rows[i]["PalletPercentageForOne"];
        //    string description = dataTableToPalletize.Rows[i]["Description"].ToString();
        //    string productionSchedule = dataTableToPalletize.Rows[i]["ProductionSchedule"].ToString();
        //    decimal maxPerPallet = 1m;
        //    bool isMPC = description.StartsWith("MPC");



        //    if ((totalQty * palletPercentageForOne * 100 >= 99.99m))
        //    {

        //        if (palletPercentageForOne != 0)
        //        {
        //            maxPerPallet = Math.Round(1 / palletPercentageForOne, 0);
        //        }

        //        if (totalQty > maxPerPallet)
        //        {
        //            dataTableToPalletize.ImportRow(dataTableToPalletize.Rows[i]);
        //            dataTableToPalletize.Rows[i]["TotalQty"] = maxPerPallet;
        //            dataTableToPalletize.Rows[i]["PalletLineNumber"] = nextPalletLineNumber;
        //            nextPalletLineNumber++;
        //            dataTableToPalletize.Rows[dataTableToPalletize.Rows.Count - 1]["TotalQty"] =
        //                (decimal)dataTableToPalletize.Rows[dataTableToPalletize.Rows.Count - 1]["TotalQty"] - maxPerPallet;
        //            dataTableToPalletize.Rows[dataTableToPalletize.Rows.Count - 1]["PalletLineNumber"] = nextPalletLineNumber;
        //        }

        //        DataSets.PalletData.PossibleStockCodesDataTable onePalletDt = new DataSets.PalletData.PossibleStockCodesDataTable();
        //        dataTableToPalletize.Rows[i]["PalletNumber"] = currentPalletNumber;
        //        onePalletDt.ImportRow(dataTableToPalletize.Rows[i]);

        //        dataTableToPalletize.Rows.RemoveAt(i);
        //        returnList.Add(onePalletDt);
        //        currentPalletNumber++;
        //    }
        //    else if (totalQty == 0)
        //    {
        //        dataTableToPalletize.Rows.RemoveAt(i);  // Remove item with zero quantity
        //    }
        //    else
        //    {
        //        i++;  // Increment index for valid rows
        //    }
        //}

        //#endregion GetFullPallets




        //ORIGINAL LOAD SECONDARY BELOW

        /// <summary>
        /// Rotates through the current pallets to see if the "secondary" group can fit.
        /// If so, it appends the items to the pallet.
        /// If not, it creates a new pallet for the group.
        /// </summary>
        /// <param name="dataTableToPalletize"></param>
        /// <param name="_Where">Should be the secondary grouping</param>
        /// <param name="_OrderBy">Should be the primary grouping</param>
        /// <param name="returnList">current list of pallets to be maintained</param>
        //private static void LoadSecondaryGroupsToPallets(DataSets.PalletData.PossibleStockCodesDataTable dataTableToPalletize, string _Where, string _OrderBy, List<DataSets.PalletData.PossibleStockCodesDataTable> returnList, int currentPalletNumber1, int nextPalletLineNumber1)
        //{
        //    // The GetDistinct returns new datatable, so this loop is not an issue on maintenance after row deletion.
        //    foreach (System.Data.DataRow dr in GetDistinctValuesInColumn(dataTableToPalletize, _Where).Rows)
        //    {
        //        // Get SubSet of Data to work with - Secondary Grouping
        //        DataRow[] drs = dataTableToPalletize.Select(string.Format("{0}='{1}'", _Where, dr["Distinct"].ToString()), _OrderBy);
        //        // Get Amount of space needed for the secondary grouping.
        //        bool FitsOnPallet = false;
        //        int currentPalletNumber = currentPalletNumber1;
        //        int nextPalletLineNumber = 1;
        //        int currentpalletnumbersv1 = 1;
        //        int PalletNumber_rowlocation = 0;
        //        //int return_listcount = returnList.Count();
        //        int counter = 0;
        //        foreach (DataSets.PalletData.PossibleStockCodesDataTable varlist in returnList)
        //        {
        //            counter++;
        //            if (CalculatePalletFilledPercentage(varlist) + CalculatePalletFilledPercentage(CopyToStronglyTypedDataTable(drs.CopyToDataTable())) <= 100)
        //            {
        //                // if sub-group does not fill pallet, add to pallet.
        //                // not certain what pallet number it is, so get that from first record.
        //                currentPalletNumber = (int)varlist[0]["PalletNumber"];
        //                nextPalletLineNumber = varlist.Rows.Count;
        //                // Need to Cycle drs records and append to returnList pallet.
        //                // need to delete each record as it finishes.    
        //                FitsOnPallet = true;
        //                break;
        //            }
        //            currentPalletNumber = (int)varlist[0]["PalletNumber"];
        //            //counter=returnList.

        //        }
        //        // Didn't fit on existing pallet, create a new one!
        //        if (!FitsOnPallet)
        //        {
        //            // create new pallet.
        //            if (returnList.Count() != 0)
        //            {
        //                currentPalletNumber++;
        //            }
        //            DataSets.PalletData.PossibleStockCodesDataTable newDataTable = new DataSets.PalletData.PossibleStockCodesDataTable();
        //            returnList.Add(newDataTable);
        //            PalletNumber_rowlocation = returnList.Count();
        //        }

        //        // Add Record to Pallet already in return List!
        //        for (int i = 0; i < drs.Count(); i++)
        //        {
        //            if (!FitsOnPallet)
        //            {

        //                drs[i]["PalletLineNumber"] = nextPalletLineNumber++;
        //                drs[i]["PalletNumber"] = currentPalletNumber;
        //                returnList[PalletNumber_rowlocation - 1].ImportRow(drs[i]);
        //            }
        //            else
        //            {

        //                drs[i]["PalletLineNumber"] = nextPalletLineNumber++;
        //                drs[i]["PalletNumber"] = currentPalletNumber;
        //                PalletNumber_rowlocation = returnList.Count();

        //                // need a logic to get the position of insertion
        //                //int a = GetPostionofpalletinreturnlist(currentPalletNumber,returnList);
        //                returnList[counter - 1].ImportRow(drs[i]);
        //            }


        //            drs[i].Delete(); // Just marks for deletion - we'll cleanup.
        //        }
        //    }
        //}




        //ORGINIAL
        //private static void ProcessLineItemsToPallets(DataSets.PalletData.PossibleStockCodesDataTable dataTableToPalletize, string _Where, string _OrderBy, int currentPalletNumber, int nextPalletLineNumber, List<DataSets.PalletData.PossibleStockCodesDataTable> returnList)
        //{
        //    // The GetDistinct returns new datatable, so this loop is not an issue on maintenance after row deletion.
        //    foreach (System.Data.DataRow dr in GetDistinctValuesInColumn(dataTableToPalletize, _Where).Rows)
        //    {
        //        // Get SubSet of Data to work with - Secondary Grouping
        //        DataRow[] drs = dataTableToPalletize.Select(string.Format("{0}='{1}'", _Where, dr["Distinct"].ToString()), _OrderBy);
        //        while (CalculatePalletFilledPercentage(CopyToStronglyTypedDataTable(drs.CopyToDataTable())) > 100)
        //        {
        //            // dataTableToPalletize needs to be modified to remove records in the new pallet.
        //            // We know we have enough to create full pallet based on Calculate of While!
        //            DataSets.PalletData.PossibleStockCodesDataTable newDataTable = new DataSets.PalletData.PossibleStockCodesDataTable();
        //            // So we'll create a pallet and check while loop again.
        //            for (int i = 0; i < drs.Count(); i++)
        //            {
        //                if (drs.Count().Equals(0)) { break; }

        //                decimal totalQty = (decimal)drs[i]["TotalQty"];
        //                decimal palletPercentageForOne = (decimal)drs[i]["PalletPercentageForOne"];

        //                if (totalQty == 0)
        //                {
        //                    drs[i].Delete(); // Just marks for deletion - we'll cleanup.
        //                    continue;
        //                }
        //                // Check if item fits before adding it, otherwise, loop to next item
        //                if (Math.Round((totalQty * palletPercentageForOne * 100) + CalculatePalletFilledPercentage(newDataTable), 4) <= 100.0m)
        //                {
        //                    drs[i]["PalletLineNumber"] = nextPalletLineNumber;
        //                    nextPalletLineNumber++;
        //                    drs[i]["PalletNumber"] = currentPalletNumber;
        //                    newDataTable.ImportRow(drs[i]);

        //                    drs[i].Delete(); // Just marks for deletion - we'll cleanup.
        //                }
        //            }

        //            returnList.Add(newDataTable);
        //            currentPalletNumber++;
        //            CleanUpDeletedRow(dataTableToPalletize);
        //            drs = dataTableToPalletize.Select(string.Format("{0}='{1}'", _Where, dr[0].ToString()), _OrderBy);
        //            if (drs.Count().Equals(0)) break;
        //        }
        //    }

        //}







        //    private static void LoadSecondaryGroupsToPallets(
        //DataSets.PalletData.PossibleStockCodesDataTable dataTableToPalletize,
        //string _Where,
        //string _OrderBy,
        //List<DataSets.PalletData.PossibleStockCodesDataTable> returnList,
        //int currentPalletNumber1,
        //int nextPalletLineNumber1)
        //    {
        //        foreach (System.Data.DataRow dr in GetDistinctValuesInColumn(dataTableToPalletize, _Where).Rows)
        //        {
        //            DataRow[] drs = dataTableToPalletize.Select(
        //                string.Format("{0}='{1}'", _Where, dr["Distinct"].ToString()),
        //                _OrderBy
        //            );

        //            bool FitsOnPallet = false;
        //            int currentPalletNumber = currentPalletNumber1;
        //            int nextPalletLineNumber = 1;
        //            int PalletNumber_rowlocation = 0;
        //            int counter = 0;

        //            // Standard processing for non-locked pallets, now no need to check for locked pallets
        //            foreach (DataSets.PalletData.PossibleStockCodesDataTable varlist in returnList)
        //            {
        //                counter++;

        //                // If the pallet is not locked, check if the rows fit within the pallet's capacity
        //                if (CalculatePalletFilledPercentage(varlist) +
        //                    CalculatePalletFilledPercentage(CopyToStronglyTypedDataTable(drs.CopyToDataTable())) <= 100)
        //                {
        //                    currentPalletNumber = (int)varlist.Rows[0]["PalletNumber"];
        //                    nextPalletLineNumber = varlist.Rows.Count;
        //                    FitsOnPallet = true;
        //                    break;
        //                }
        //            }

        //            // Create a new pallet if the rows don't fit into existing pallets
        //            if (!FitsOnPallet)
        //            {
        //                if (returnList.Count() != 0)
        //                {
        //                    currentPalletNumber++;
        //                }
        //                DataSets.PalletData.PossibleStockCodesDataTable newDataTable =
        //                    new DataSets.PalletData.PossibleStockCodesDataTable();
        //                returnList.Add(newDataTable);
        //                PalletNumber_rowlocation = returnList.Count();
        //            }

        //            // Add records to the appropriate pallet
        //            for (int i = 0; i < drs.Count(); i++)
        //            {
        //                if (!FitsOnPallet)
        //                {
        //                    drs[i]["PalletLineNumber"] = nextPalletLineNumber++;
        //                    drs[i]["PalletNumber"] = currentPalletNumber;
        //                    returnList[PalletNumber_rowlocation - 1].ImportRow(drs[i]);
        //                }
        //                else
        //                {
        //                    drs[i]["PalletLineNumber"] = nextPalletLineNumber++;
        //                    drs[i]["PalletNumber"] = currentPalletNumber;
        //                    returnList[counter - 1].ImportRow(drs[i]);
        //                }
        //                drs[i].Delete();
        //            }
        //        }

        //    }



        /// <summary>
        /// Removes rows flagged for deletion.  Flagging for deletion allows loop to continue without indexing modifications... continues to next not --i
        /// JPS Function 2016-05-25
        /// </summary>
        /// <param name="dt">DataTable that we are checking the RowStates</param>
        private static void CleanUpDeletedRow(System.Data.DataTable dt)
        {
            for (Int32 i = dt.Rows.Count - 1; i >= 0; i--)
                if (dt.Rows[i].RowState.Equals(System.Data.DataRowState.Deleted))
                    dt.Rows.RemoveAt(i);
        }
        /// <summary>
        /// JPS - Added to narrow scope of records to query at a time... looping / creating pallet based on SecondaryGrouping.
        /// </summary>
        /// <param name="dataTableToPalletize"></param>
        /// <param name="_Where"></param>
        /// <returns></returns>
        private static DataTable GetDistinctValuesInColumn(DataSets.PalletData.PossibleStockCodesDataTable dataTableToPalletize, string _Where)
        {
            var results = (from row in dataTableToPalletize.AsEnumerable()
                          
                          group row by new { Grouper = row.Field<int>(_Where) } into rowGroup
                          select new { Distinct = rowGroup.Key.Grouper, 
                                       Sum= rowGroup.Sum(r=>r.Field<Decimal>("TotalSpaceNeeded")) }).OrderByDescending(t=>t.Sum);
            DataTable queryResults = new DataTable();
            queryResults.Columns.Add("Distinct", typeof(Int32));
            queryResults.Columns.Add("Sum", typeof(Decimal));
            
            foreach (var result in results)
                queryResults.Rows.Add(new object[] { result.Distinct,result.Sum });
            return queryResults;
        }

        private static DataSets.PalletData.PossibleStockCodesDataTable CopyToStronglyTypedDataTable(DataTable dt)
        {

            DataSets.PalletData.PossibleStockCodesDataTable tmpTable = new DataSets.PalletData.PossibleStockCodesDataTable();

            foreach (DataRow dr in dt.Rows)
            {
                tmpTable.ImportRow(dr);
            }
            return tmpTable;
        }
        private static void ConvertToStronglyTypedDataTable(DataTable dt, out DataSets.PalletData.PossibleStockCodesDataTable dataTableIn)
        {
            dataTableIn = new DataSets.PalletData.PossibleStockCodesDataTable();

            foreach (DataRow dr in dt.Rows)
            {
                dataTableIn.ImportRow(dr);
            }
        }


        private static decimal CalculatePalletFilledPercentage(DataSets.PalletData.PossibleStockCodesDataTable pallet)
        {

            decimal filledPcnt = 0m;

            foreach (DataRow dr in pallet.Rows)
            {
                decimal drTotalQty = (decimal)dr["TotalQty"];
                decimal drPalletPercentageForOne = (decimal)dr["PalletPercentageForOne"];

                filledPcnt += (drTotalQty * drPalletPercentageForOne);
            }

            return filledPcnt * 100;


        }

        //private static int GetPostionofpalletinreturnlist(int palletnumber, List<DataSets.PalletData.PossibleStockCodesDataTable> returnList)
        //{
        //    int position = 0;
        //    int counter
        //    foreach (DataSets.PalletData.PossibleStockCodesDataTable varlist in returnList)
        //    {
        //        if((int)varlist[0]["PalletNumber"])
        //        {

        //        }


        //    }
        //    return position;
        //}





        }
}


