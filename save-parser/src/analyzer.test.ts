import assert from "node:assert/strict";
import test from "node:test";
import { analyzeParsedSave } from "./analyzer.js";

const buildable = (typePath: string, x: number, y: number) => ({
  typePath,
  needTransform: true,
  transform: { translation: { x, y, z: 0 } },
});

test("summarizes buildings and detects separated factory areas", () => {
  const constructor =
    "/Game/FactoryGame/Buildable/Factory/ConstructorMk1/Build_ConstructorMk1.Build_ConstructorMk1_C";
  const foundation =
    "/Game/FactoryGame/Buildable/Building/Foundation/Build_Foundation.Build_Foundation_C";
  const save = {
    header: {
      saveName: "Automation",
      sessionName: "Friends",
      saveVersion: 52,
      buildVersion: 123456,
      isModdedSave: 1,
    },
    levels: {
      Persistent_Level: {
        objects: [
          buildable(constructor, 0, 0),
          buildable(constructor, 1_000, 0),
          buildable(constructor, 2_000, 0),
          buildable(foundation, 500, 500),
          buildable(constructor, 100_000, 100_000),
          buildable(constructor, 101_000, 100_000),
          buildable(constructor, 102_000, 100_000),
        ],
      },
    },
  };

  const result = analyzeParsedSave(save, "fallback.sav");

  assert.equal(result.saveName, "Automation");
  assert.equal(result.totals.objects, 7);
  assert.equal(result.totals.buildables, 7);
  assert.equal(result.totals.productionMachines, 6);
  assert.equal(result.isModdedSave, true);
  assert.equal(result.totals.foundations, 1);
  assert.equal(result.detectedAreas.length, 2);
  assert.equal(result.detectedAreas[0]?.machineCount, 3);
  assert.ok((result.detectedAreas[0]?.buildableCount ?? 0) >= 3);
});

test("returns an empty but valid summary for unknown save content", () => {
  const result = analyzeParsedSave({}, "Unknown.sav");

  assert.equal(result.saveName, "Unknown.sav");
  assert.equal(result.totals.objects, 0);
  assert.equal(result.totals.buildables, 0);
  assert.deepEqual(result.detectedAreas, []);
});
