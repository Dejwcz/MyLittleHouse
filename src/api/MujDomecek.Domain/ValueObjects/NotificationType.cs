namespace MujDomecek.Domain.ValueObjects;

public enum NotificationType
{
    InvitationReceived = 0,
    InvitationAccepted = 1,
    InvitationExpired = 2,
    MentionInComment = 3,
    CommentOnYourZaznam = 4,
    DraftReminder = 5,
    DraftExpiring = 6,
    InvitationDeclined = 7
}
