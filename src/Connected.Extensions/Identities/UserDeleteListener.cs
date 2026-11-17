using Connected.Annotations;
using Connected.Entities;
using Connected.Identities.Authentication;
using Connected.Identities.Authentication.Dtos;
using Connected.Notifications;
using Connected.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Connected.Identities;

[Middleware<IUserService>(ServiceEvents.Deleted)]
internal sealed  class UserDeleteListener(IIdentityAuthenticationTokenService identityAuthenticationTokenService)
	: EventListener<IPrimaryKeyDto<long>>
{
	protected override async Task OnInvoke()
	{
		var user = Sender.GetState<IUser>().Required();

		await identityAuthenticationTokenService.Revoke(user.Token);
	}
}
