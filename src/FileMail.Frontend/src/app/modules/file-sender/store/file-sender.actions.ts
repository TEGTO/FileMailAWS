
import { createAction, props } from "@ngrx/store";
import { SendFileRequest } from "..";

export const sendFile = createAction(
    '[File-Sender] Send File',
    props<{ req: SendFileRequest }>()
);

export const sendFileSuccess = createAction(
    '[File-Sender] Send File Success'
);