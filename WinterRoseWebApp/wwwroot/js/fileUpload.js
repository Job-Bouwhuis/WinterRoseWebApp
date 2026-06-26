export function getRelativePaths(inputId)
{
    const input = document.getElementById(inputId);
    if (!input?.files) return [];
    return Array.from(input.files).map(f => f.webkitRelativePath || f.name);
}