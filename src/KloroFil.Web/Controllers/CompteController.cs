using KloroFil.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KloroFil.Web.Controllers;

public class CompteController(UserManager<Utilisateur> utilisateurs, SignInManager<Utilisateur> connexions) : Controller
{
    [AllowAnonymous]
    public async Task<IActionResult> Connexion(string? returnUrl)
    {
        if (!await utilisateurs.Users.AnyAsync()) return RedirectToAction(nameof(Initialisation));
        ViewBag.ReturnUrl = returnUrl;
        return View(new ConnexionVm());
    }

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Connexion(ConnexionVm vm, string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        if (!ModelState.IsValid) return View(vm);

        var resultat = await connexions.PasswordSignInAsync(vm.Email.Trim(), vm.MotDePasse, vm.SeSouvenir, lockoutOnFailure: true);
        if (resultat.Succeeded)
            return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction("Index", "Home");

        // Même message dans tous les cas, pour ne pas révéler quelles adresses existent.
        ModelState.AddModelError("", "Email ou mot de passe incorrect, ou compte bloqué.");
        return View(new ConnexionVm { Email = vm.Email, SeSouvenir = vm.SeSouvenir });
    }

    /// <summary>Création du premier administrateur : disponible uniquement tant qu'aucun compte n'existe.</summary>
    [AllowAnonymous]
    public async Task<IActionResult> Initialisation() =>
        await utilisateurs.Users.AnyAsync() ? RedirectToAction(nameof(Connexion)) : View(new NouvelUtilisateurVm());

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Initialisation(NouvelUtilisateurVm vm)
    {
        if (await utilisateurs.Users.AnyAsync()) return RedirectToAction(nameof(Connexion));
        if (!ModelState.IsValid) return View(vm);

        var email = vm.Email.Trim();
        var compte = new Utilisateur { UserName = email, Email = email, EmailConfirmed = true, NomComplet = vm.NomComplet, Fonction = vm.Fonction };
        var resultat = await utilisateurs.CreateAsync(compte, vm.MotDePasse);
        if (resultat.Succeeded)
            resultat = await utilisateurs.AddToRoleAsync(compte, RolesKloroFil.Administrateur);
        if (!resultat.Succeeded)
        {
            foreach (var e in resultat.Errors) ModelState.AddModelError("", e.Description);
            return View(vm);
        }

        await connexions.SignInAsync(compte, isPersistent: false);
        TempData["Succes"] = "Compte administrateur créé. Bienvenue dans KloroFil !";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Deconnexion()
    {
        await connexions.SignOutAsync();
        return RedirectToAction(nameof(Connexion));
    }

    public IActionResult MotDePasse() => View(new ChangerMotDePasseVm());

    [HttpPost]
    public async Task<IActionResult> MotDePasse(ChangerMotDePasseVm vm)
    {
        if (!ModelState.IsValid) return View(new ChangerMotDePasseVm());

        var compte = await utilisateurs.GetUserAsync(User);
        if (compte is null) return Challenge();

        var resultat = await utilisateurs.ChangePasswordAsync(compte, vm.Actuel, vm.Nouveau);
        if (!resultat.Succeeded)
        {
            foreach (var e in resultat.Errors) ModelState.AddModelError("", e.Description);
            return View(new ChangerMotDePasseVm());
        }

        await connexions.RefreshSignInAsync(compte);
        TempData["Succes"] = "Mot de passe modifié.";
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public IActionResult AccesRefuse() => View();
}
