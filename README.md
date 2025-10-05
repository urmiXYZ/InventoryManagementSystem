# Inventory Management System  

An **Inventory Management System** built with **ASP.NET Core MVC, Entity Framework Core, and Bootstrap** to handle suppliers, products, orders, customers, and delivery workflows. The system supports CRUD operations, AJAX-based interactions, modals, and real-time UI updates.  

---

## 🚀 Features Implemented  

### 🔹 Category Management  
- Add, edit, delete categories  
- AJAX-based delete with confirmation modal  
- Prevent deletion if linked products exist  (alerts and gives option to make the move the products to "Uncategorized" category first before deleting the category)
  
### 🔹 Supplier Management  
- Add, edit, delete suppliers  
- AJAX-based delete with confirmation modal  
- Prevent deletion if linked products exist  (alerts and gives option to make the supplier inactive)

### 🔹 Product Management  
- Add, edit, delete products  
- Products linked with categories and suppliers  
- AJAX CRUD with toast notifications  

### 🔹 Customer Management  
- Customer CRUD operations  
- Integrated into product ordering process  

### 🔹 Order Management  
- Create and edit orders with product details  
- Update order status (InProgress, Processing, InDelivery, Delivered, Cancelled)  
- “Send for Delivery” modal (option of whole or partial deliver of product)   

### 🔹 Delivery Workflow  
- Modal for sending orders for delivery  
- Status-based UI changes (buttons disabled, rows greyed out)  

### 🔹 UI & UX Enhancements  
- Bootstrap 5 design with modals and form controls  
- Toast notifications for success/error feedback  
- Confirmation modals for delete actions  

---

## 🛠️ Tech Stack  

- **Backend**: ASP.NET Core MVC (C#), Entity Framework Core  
- **Frontend**: Bootstrap 5, jQuery, AJAX  
- **Database**: SQL Server (EF Core Code-First)  

---


