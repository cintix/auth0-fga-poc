# OpenFGA PoC: Fine-Grained Authorization for Multi-Account Services

## Overview

This PoC demonstrates how to implement **fine-grained access control** using **OpenFGA** in a **multi-account, multi-workspace** setup.  
The goal is to show how **roles** and **permissions** propagate through the hierarchy, how **inheritance** works, and how **access checks** can be performed for different users on various resources.

---

## Authorization Model

### Objects

- **Account** – top-level container, owned by a User.
- **Workspace** – belongs to an Account.
- **PpgPolicy** – belongs to a Workspace.
- **CmpConfiguration** – belongs to a Workspace.

### Roles

| Role     | Permissions                       |
|----------|-----------------------------------|
| Reader   | READ only                         |
| Editor   | READ + WRITE                      |
| Reviewer | REVIEW (only on Configurations)   |
| Admin    | FULL access (implies all actions) |

### Role Inheritance

- **Account → Workspace → Policy / Configuration**
    - Admin/Editor/Reader on an **Account** is automatically the same role on all **Workspaces** and their **Policies/Configurations**.
    - Admin/Editor/Reader on a **Workspace** is automatically the same on its **Policies/Configurations**.
    - Direct role assignments are respected at the resource level.

- **Reviewer** is a special role:
    - Can be assigned directly on a **CmpConfiguration**.
    - Also inherited by **Admins**.

---

## Test Data

We created 4 users, 2 accounts, each with 1 workspace, and each workspace contains 2 policies and 2 configurations.

### Users

- **Bob**
- **Sara**
- **Jenny**
- **Geo**

### Accounts & Assignments

| Account   | Workspace   | Users & Roles                  |
|-----------|-------------|--------------------------------|
| Account 1 | Workspace 1 | Bob (Admin), Jenny (Editor on Workspace1) |
| Account 2 | Workspace 2 | Sara (Admin), Geo (Reader on Workspace2) |

### Resources

| Resource              | Parent       | Users & Roles                         |
|-----------------------|--------------|---------------------------------------|
| PpgPolicy 1a          | Workspace 1  | Sara (Editor direct), Bob (Admin inherited) |
| PpgPolicy 1b          | Workspace 1  | Bob (Admin inherited)                 |
| PpgPolicy 2a          | Workspace 2  | Sara (Admin inherited)                |
| PpgPolicy 2b          | Workspace 2  | Sara (Admin inherited)                |
| CmpConfiguration 1a   | Workspace 1  | Bob (Admin inherited)                 |
| CmpConfiguration 1b   | Workspace 1  | Bob (Admin inherited)                 |
| CmpConfiguration 2a   | Workspace 2  | Bob (Reviewer direct), Sara (Admin inherited) |
| CmpConfiguration 2b   | Workspace 2  | Sara (Admin inherited)                |

---

## What We Are Testing

1. **Role inheritance**
    - Admin on Account → Admin on all Workspaces, Policies, and Configurations.
    - Editor on Workspace → Editor on Policies/Configurations under that Workspace.
    - Reader on Workspace → Reader on Policies/Configurations under that Workspace.

2. **Direct assignments**
    - Sara is Editor only on Policy1a.
    - Bob is Reviewer only on Configuration2a.

3. **Access isolation**
    - Users from Account1 cannot access resources under Account2 unless explicitly assigned (and vice versa).

4. **Check-access scenarios**
    - Validate that Admin/Editor/Reader/Reviewer permissions match expectations for each resource.

---

## Example Access Checks

| User  | Resource              | Expected Role          | Expected Access      |
|-------|-----------------------|------------------------|----------------------|
| Bob   | Policy1a              | Admin (inherited)      | ✅ Full access       |
| Sara  | Policy1a              | Editor (direct)        | ✅ Read + Write      |
| Jenny | Policy1b              | Editor (inherited)     | ✅ Read + Write      |
| Geo   | Policy2b              | Reader (inherited)     | ✅ Read only         |
| Bob   | Configuration2a       | Reviewer (direct)      | ✅ Review allowed    |
| Sara  | Configuration2a       | Admin (inherited)      | ✅ Full access       |

---

## Temporary Access (Conditional Roles)

In addition to permanent role assignments, this PoC demonstrates **temporary role grants** using **conditions** in the authorization model.

We introduced a condition:

```fga
condition temporary_user_grant(current_time: timestamp, grant_time: timestamp, grant_duration: duration) {
  current_time < grant_time + grant_duration
}
