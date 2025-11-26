Feature: MyFeature

Scenario: Perform recorded actions on MyFeature
	Given I navigate to "https://todomvc.com/examples/react/dist/"
	And I type "learn to code" and press Enter in element with CssSelector "[data-testid="text-input"]"
	And I type "write a letter" and press Enter in element with CssSelector "[data-testid="text-input"]"
	And I type "clean the house" and press Enter in element with CssSelector "[data-testid="text-input"]"
	And I type "this is a task" and press Enter in element with CssSelector "[data-testid="text-input"]"
	When I click the element with XPath "//label[normalize-space()="write a letter"]/preceding-sibling::input"
	Then the page should be in the expected state
