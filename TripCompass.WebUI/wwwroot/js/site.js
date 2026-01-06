// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener("DOMContentLoaded", function () {
    const dropdown = document.querySelector(".user-dropdown");
    const toggle = dropdown?.querySelector(".dropdown-toggle");

    toggle?.addEventListener("click", function (e) {
        e.preventDefault();
        dropdown.classList.toggle("open");
    });

    document.addEventListener("click", function (e) {
        if (!dropdown.contains(e.target)) {
            dropdown.classList.remove("open");
        }
    });
});
