### 📦 Auto-Palletization System (C# & SQL Server)
**Purpose:** Developed and optimized an algorithm to automatically assign and organize sales order items onto pallets for warehouse operations.  

**Tech Stack:** C# .NET, SQL Server (Stored Procedures), Visual Studio 2022  

**Key Contributions:**
- Designed and implemented the `AutoPalletizeShipment` method in C#, grouping items by product type, quantity, and customer rules.  
- Built dynamic logic to split items into **full pallets** and **partial pallets** while maintaining order accuracy.  
- Refactored and optimized a SQL Server stored procedure (`PalletMasterProcedure`) to mirror the C# palletization rules.  
- Handled **special business rules** for specific customers (e.g., Singer – Customer 10457) requiring unique product/quantity handling.  
- Improved database performance by **reducing query complexity** and applying indexing best practices.  

**Value:** Automated pallet assignment reduced manual labor, improved order accuracy, and increased warehouse throughput.  
