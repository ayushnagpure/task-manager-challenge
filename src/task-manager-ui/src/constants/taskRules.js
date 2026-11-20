// Field limits mirrored from the API (src/TaskManager.Api/Data/Configurations/TaskItemConfiguration.cs).
// The server is the source of truth: these only let the form catch obvious problems before submitting.
export const TITLE_MAX = 200;
export const DESCRIPTION_MAX = 2000;
export const ASSIGNED_TO_MAX = 100;
