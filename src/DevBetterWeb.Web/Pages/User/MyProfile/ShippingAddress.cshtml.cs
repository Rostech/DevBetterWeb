﻿using System.Threading.Tasks;
using DevBetterWeb.Core;
using DevBetterWeb.Core.Entities;
using DevBetterWeb.Core.Exceptions;
using DevBetterWeb.Core.Interfaces;
using DevBetterWeb.Core.Specs;
using DevBetterWeb.Core.ValueObjects;
using DevBetterWeb.Infrastructure.Identity.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DevBetterWeb.Web.Pages.User
{
	[Authorize(Roles = AuthConstants.Roles.ADMINISTRATORS_MEMBERS_ALUMNI)]
	public class ShippingAddressModel : PageModel
	{
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly IMemberRegistrationService _memberRegistrationService;
		private readonly IRepository<Member> _memberRepository;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		public ShippingAddressModel(UserManager<ApplicationUser> userManager,
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
			IMemberRegistrationService memberRegistrationService,
			IRepository<Member> memberRepository)
		{
			_userManager = userManager;
			_memberRegistrationService = memberRegistrationService;
			_memberRepository = memberRepository;
		}

		[BindProperty]
		public UserShippingAddressUpdateModel UserShippingAddressUpdateModel { get; set; }

		public async Task OnGetAsync()
		{
			var currentUserName = User.Identity!.Name;
			var applicationUser = await _userManager.FindByNameAsync(currentUserName);

			var spec = new MemberByUserIdSpec(applicationUser.Id);
			var member = await _memberRepository.FirstOrDefaultAsync(spec);

			if (member == null)
			{
				member = await _memberRegistrationService.RegisterMemberAsync(applicationUser.Id);
			}

			UserShippingAddressUpdateModel = new UserShippingAddressUpdateModel(member);
		}

		public async Task OnPost()
		{
			if (!ModelState.IsValid) return;

			var currentUserName = User.Identity!.Name;
			var applicationUser = await _userManager.FindByNameAsync(currentUserName);

			var spec = new MemberByUserIdSpec(applicationUser.Id);
			var member = await _memberRepository.FirstOrDefaultAsync(spec);
			if (member is null) throw new MemberNotFoundException(applicationUser.Id);

			member.UpdateShippingAddress(new Address(UserShippingAddressUpdateModel.Street,
				UserShippingAddressUpdateModel.City,
				UserShippingAddressUpdateModel.State,
				UserShippingAddressUpdateModel.PostalCode,
				UserShippingAddressUpdateModel.Country));

#pragma warning disable CS8602 // Dereference of a possibly null reference.
			member.UpdateAddress(member.ShippingAddress.ToString());
#pragma warning restore CS8602 // Dereference of a possibly null reference.

			await _memberRepository.UpdateAsync(member);
		}
	}
}