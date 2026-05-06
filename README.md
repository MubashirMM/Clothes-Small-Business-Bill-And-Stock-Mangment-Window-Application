# Clothes Business Billing & Stock Management System

A professional Windows desktop application built with **WPF (.NET)** following the **MVVM (Model-View-ViewModel)** architectural pattern. This system is designed for retail clothing businesses to manage stock, handle point-of-sale (POS) operations, and analyze sales performance through a multi-tier role-based system.

## 🏗 Architecture (MVVM)
This project implements a strict separation of concerns:
- **Models:** Data structures for Products, Bills, and Users.
- **Views:** XAML-based UI with data-bound components.
- **ViewModels:** Business logic and command handling (`ICommand`), ensuring the UI remains decoupled from the logic.

---

## 🔐 Role-Based Access Control (RBAC)

### 👑 Super Admin (Analytics & Oversight)
*   **Sales Dashboard:** High-level overview of daily, weekly, and monthly sales.
*   **Performance Tracking:** Identification of top-selling items and revenue trends.
*   **Inventory Oversight:** Global access to manage and reconcile stock levels.

### 🛠 Admin (Inventory Management)
*   **Product CRUD:** Create, Read, Update, and Delete clothing items.
*   **Category Management:** Organize items by type, size, or brand.
*   **Live Stock Tracking:** Monitor current inventory counts in real-time.

### 👤 User (Sales & Billing)
*   **Digital Cart:** Fast and intuitive item selection for customers.
*   **Automated Billing:** Generate professional invoices and bills instantly.
*   **Transaction History:** View and track personal billing history for customer service.

---

## 🛠 Tech Stack
- **Framework:** WPF (.NET)
- **Pattern:** MVVM (Model-View-ViewModel)
- **Language:** C#
- **UI:** XAML with Data Binding and Styles
- **Data:** SQL/Local DB Integration

---

## 🚀 Installation & Setup

1. **Clone the repository:**
   ```bash
   git clone [https://github.com/MubashirMM/Clothes-Small-Business-Bill-And-Stock-Mangment-Window-Application.git](https://github.com/MubashirMM/Clothes-Small-Business-Bill-And-Stock-Mangment-Window-Application.git)