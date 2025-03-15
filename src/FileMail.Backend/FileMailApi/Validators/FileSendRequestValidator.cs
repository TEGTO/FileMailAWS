using FileMailApi.Endpoints.File;
using FluentValidation;

namespace FileMailApi.Validators
{
    public class FileSendRequestValidator : AbstractValidator<FileSendRequest>
    {
        public FileSendRequestValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
            RuleFor(x => x.File).NotEmpty();
        }
    }
}
