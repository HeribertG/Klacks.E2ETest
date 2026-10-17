// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.E2ETest.Constants;

/// <summary>
/// Names, API paths and SQL for the throw-away container template the time ruler tests create and remove again.
/// Shifts are posted as sealed orders (POST Shifts only accepts orders); the API answers with the plannable copy
/// (status OriginalShift) it creates, so the cleanup removes the copies and the sealed orders they point to via
/// original_id. A sealed task without a customer needs a group, so a throw-away root group is created as well.
/// </summary>
public static class ContainerTemplateTestDataIds
{
    public const string NamePrefix = "INTEGRATION_TEST_";
    public const string ContainerName = NamePrefix + "CONTAINER";
    public const string ContainerAbbreviation = "ITC";
    public const string TaskName = NamePrefix + "TASK";
    public const string TaskAbbreviation = "ITT";
    public const string GroupName = NamePrefix + "GROUP";
    public const string GroupValidFrom = "2020-01-01T00:00:00Z";

    public const string DefaultApiBaseUrl = "https://localhost:5001/api/backend/";
    public const string ApiBaseUrlEnvVar = "KLACKS_E2E_APIURL";
    public const string LoginPath = "Accounts/LoginUser";
    public const string ShiftsPath = "Shifts";
    public const string GroupsPath = "Groups";
    public const string GroupPathPrefix = "Groups/";
    public const string ContainersPath = "Containers/";
    public const string LockAcquirePath = "ContainerLocks/Acquire";
    public const string LockReleasePath = "ContainerLocks/";
    public const string LockResourceType = "ContainerTemplate";
    public const string InstanceIdHeader = "X-Instance-Id";
    public const string InstanceIdPrefix = "e2e-setup-";
    public const string BearerScheme = "Bearer";
    public const string TokenJsonKey = "token";
    public const string IdJsonKey = "id";

    public const int ContainerShiftType = 1;
    public const int TaskShiftType = 0;
    public const int SealedOrderStatus = 1;
    public const int MondayWeekday = 1;
    public const int ShiftWorkTime = 12;
    public const string ShiftFromDate = "2020-01-01";
    public const string ContainerStart = "06:00:00";
    public const string ContainerEnd = "18:00:00";
    public const string ZeroTime = "00:00:00";
    public const string FirstItemStart = "08:00:00";
    public const string FirstItemEnd = "10:00:00";
    public const string SecondItemStart = "12:00:00";
    public const string SecondItemEnd = "14:00:00";

    public const string ContainerParameter = "container";
    public const string TaskParameter = "task";
    public const string NamePrefixParameter = "prefix";
    public const string GroupParameter = "group";

    private const string CreatedShifts = @"(SELECT id FROM shift
   WHERE (id IN (@container, @task) OR id IN (SELECT original_id FROM shift WHERE id IN (@container, @task)))
     AND name LIKE @prefix)";

    public const string CleanupSql = @"
DELETE FROM container_template_item
 WHERE shift_id IN " + CreatedShifts + @"
    OR container_template_id IN (SELECT id FROM container_template WHERE container_id IN " + CreatedShifts + @");
DELETE FROM container_template WHERE container_id IN " + CreatedShifts + @";
DELETE FROM container_lock WHERE resource_id IN " + CreatedShifts + @";
DELETE FROM group_item WHERE shift_id IN " + CreatedShifts + @"
    OR group_id IN (SELECT id FROM ""group"" WHERE id = @group AND name LIKE @prefix);
DELETE FROM shift WHERE id IN " + CreatedShifts + @";
DELETE FROM group_visibility WHERE group_id IN (SELECT id FROM ""group"" WHERE id = @group AND name LIKE @prefix);
DELETE FROM ""group"" WHERE id = @group AND name LIKE @prefix;";
}
