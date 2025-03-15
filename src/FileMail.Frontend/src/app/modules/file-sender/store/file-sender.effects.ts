import { Injectable } from "@angular/core";
import { Actions, createEffect, ofType } from "@ngrx/effects";
import { map, of, switchMap } from "rxjs";
import { FileSenderApiService, sendFile, sendFileSuccess } from "..";
import { SnackbarManager } from "../../shared";

@Injectable({
    providedIn: 'root'
})
export class FileSenderEffects {

    constructor(
        private readonly actions$: Actions,
        private readonly fileSenderApiService: FileSenderApiService,
        private readonly snackbarManager: SnackbarManager
    ) { }

    sendFile$ = createEffect(() =>
        this.actions$.pipe(
            ofType(sendFile),
            switchMap((action) =>
                this.fileSenderApiService.sendFile(action.req).pipe(
                    map(() => {
                        return sendFileSuccess();
                    }),
                )
            )
        )
    );

    sendFileSuccess$ = createEffect(() =>
        this.actions$.pipe(
            ofType(sendFileSuccess),
            switchMap(() => {
                this.snackbarManager.openInfoSnackbar("✅ File sent", 3);
                return of();
            })
        ),
        { dispatch: false }
    );
}