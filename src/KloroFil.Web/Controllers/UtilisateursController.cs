using KloroFil.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KloroFil.Web.Controllers;

[Authorize(Roles = RolesKloroFil.Administrateur)]
public class UtilisateursController(UserManager<Utilisateur> utilisateurs) : Controller
{
    public async Task<IActionResult> Index()
    {
        var lignes = new List<LigneUtilisateur>();
        foreach (var u in await utilisateurs.Users.AsNoTracking().OrderBy(u => u.Email).ToListAsync())
            lignes.Add(new LigneUtilisateur(u.Id, u.Email ?? u.UserName!, u.NomComplet, u.Fonction, await RoleAsync(u), EstActif(u)));
        return View(lignes);
    }

    public IActionResult Create() => View(new NouvelUtilisateurVm());

    [HttpPost]
    public async Task<IActionResult> Create(NouvelUtilisateurVm vm)
    {
        if (!RolesKloroFil.Tous.Contains(vm.Role)) ModelState.AddModelError(nameof(vm.Role), "Rôle inconnu.");
        if (!ModelState.IsValid) return View(vm);

        var email = vm.Email.Trim();
        var compte = new Utilisateur { UserName = email, Email = email, EmailConfirmed = true, NomComplet = vm.NomComplet, Fonction = vm.Fonction };
        var resultat = await utilisateurs.CreateAsync(compte, vm.MotDePasse);
        if (resultat.Succeeded)
            resultat = await utilisateurs.AddToRoleAsync(compte, vm.Role);
        if (!Reussi(resultat)) return View(vm);

        TempData["Succes"] = $"Compte « {email} » créé.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var compte = await utilisateurs.FindByIdAsync(id);
        if (compte is null) return NotFound();
        return View(new EditionUtilisateurVm
        {
            Id = compte.Id,
            Email = compte.Email ?? compte.UserName!,
            NomComplet = compte.NomComplet,
            Fonction = compte.Fonction,
            Role = await RoleAsync(compte),
            Actif = EstActif(compte)
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(string id, EditionUtilisateurVm vm)
    {
        var compte = await utilisateurs.FindByIdAsync(id);
        if (compte is null) return NotFound();
        vm.Id = compte.Id;
        vm.Email = compte.Email ?? compte.UserName!;

        if (!RolesKloroFil.Tous.Contains(vm.Role)) ModelState.AddModelError(nameof(vm.Role), "Rôle inconnu.");
        if (!ModelState.IsValid) return View(vm);

        compte.NomComplet = vm.NomComplet;
        compte.Fonction = vm.Fonction;
        if (!Reussi(await utilisateurs.UpdateAsync(compte))) return View(vm);

        // On ne peut ni se retirer ses propres droits ni se bloquer : il reste donc toujours un administrateur.
        if (!EstMoi(compte))
        {
            var roles = await utilisateurs.GetRolesAsync(compte);
            if (!roles.SequenceEqual([vm.Role]))
            {
                if (!Reussi(await utilisateurs.RemoveFromRolesAsync(compte, roles))) return View(vm);
                if (!Reussi(await utilisateurs.AddToRoleAsync(compte, vm.Role))) return View(vm);
            }

            if (vm.Actif != EstActif(compte))
            {
                await utilisateurs.SetLockoutEndDateAsync(compte, vm.Actif ? null : DateTimeOffset.MaxValue);
                if (vm.Actif) await utilisateurs.ResetAccessFailedCountAsync(compte);
            }

            // Applique immédiatement le changement aux sessions ouvertes.
            await utilisateurs.UpdateSecurityStampAsync(compte);
        }

        TempData["Succes"] = $"Compte « {vm.Email} » enregistré.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> MotDePasse(string id)
    {
        var compte = await utilisateurs.FindByIdAsync(id);
        return compte is null ? NotFound() : View(new NouveauMotDePasseVm { Id = compte.Id, Email = compte.Email ?? compte.UserName! });
    }

    [HttpPost]
    public async Task<IActionResult> MotDePasse(string id, NouveauMotDePasseVm vm)
    {
        var compte = await utilisateurs.FindByIdAsync(id);
        if (compte is null) return NotFound();
        var formulaire = new NouveauMotDePasseVm { Id = compte.Id, Email = compte.Email ?? compte.UserName! };
        if (!ModelState.IsValid) return View(formulaire);

        var jeton = await utilisateurs.GeneratePasswordResetTokenAsync(compte);
        if (!Reussi(await utilisateurs.ResetPasswordAsync(compte, jeton, vm.MotDePasse))) return View(formulaire);

        TempData["Succes"] = $"Mot de passe de « {formulaire.Email} » réinitialisé.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var compte = await utilisateurs.FindByIdAsync(id);
        if (compte is null) return NotFound();
        if (EstMoi(compte))
        {
            TempData["Erreur"] = "Vous ne pouvez pas supprimer votre propre compte.";
            return RedirectToAction(nameof(Index));
        }

        var resultat = await utilisateurs.DeleteAsync(compte);
        TempData[resultat.Succeeded ? "Succes" : "Erreur"] = resultat.Succeeded
            ? $"Compte « {compte.Email} » supprimé."
            : string.Join(" ", resultat.Errors.Select(e => e.Description));
        return RedirectToAction(nameof(Index));
    }

    private bool EstMoi(Utilisateur compte) => compte.Id == utilisateurs.GetUserId(User);

    private static bool EstActif(Utilisateur compte) => compte.LockoutEnd is null || compte.LockoutEnd <= DateTimeOffset.UtcNow;

    private async Task<string> RoleAsync(Utilisateur compte) =>
        (await utilisateurs.GetRolesAsync(compte)).FirstOrDefault() ?? RolesKloroFil.Lecteur;

    private bool Reussi(IdentityResult resultat)
    {
        foreach (var e in resultat.Errors) ModelState.AddModelError("", e.Description);
        return resultat.Succeeded;
    }
}
