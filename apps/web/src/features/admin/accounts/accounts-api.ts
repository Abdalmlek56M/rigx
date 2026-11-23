import { unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";

export type MoneyAccount = Schemas["MoneyAccountView"];
export type MoneyAccountRequest = Schemas["MoneyAccountRequest"];

const api = () => browserApi();
const path = (id: string) => ({ params: { path: { id } } });

/** Every account with its balance, switched-off ones too. */
export const fetchAccounts = () => required(unwrap(api().GET("/api/v1/admin/retail/money/accounts")));

export const createAccount = (body: MoneyAccountRequest) =>
  required(unwrap(api().POST("/api/v1/admin/retail/money/accounts", { body })));

export const updateAccount = (id: string, body: MoneyAccountRequest) =>
  required(unwrap(api().PUT("/api/v1/admin/retail/money/accounts/{id}", { ...path(id), body })));

export type MoneyMovement = Schemas["MoneyMovementView"];
export type MoneyTransferRequest = Schemas["MoneyTransferRequest"];

/** The newest 500 movements of one account (a transfer shows as `TransferIn` / `TransferOut`). */
export const fetchMovements = (id: string) =>
  required(unwrap(api().GET("/api/v1/admin/retail/money/accounts/{id}/movements", { params: { path: { id }, query: {} } })));

/** Moves money between two different accounts; the server refuses (409) when the source balance is too small. It is neither an expense nor an income. */
export const transferMoney = async (body: MoneyTransferRequest) => {
  await unwrap(api().POST("/api/v1/admin/retail/money/transfers", { body }));
};

/** Only an account that was never used; the server refuses (409) otherwise and the account is deactivated instead. */
export const deleteAccount = async (id: string) => {
  await unwrap(api().DELETE("/api/v1/admin/retail/money/accounts/{id}", path(id)));
};
